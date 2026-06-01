using Kaligraphy.Parsing;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Text;
using Konnect.Contract.Plugin.Game;
using Konnect.Contract.Progress;
using Konnect.DataClasses.Management.Batch;
using Konnect.DataClasses.Management.Text;
using Konnect.Extensions;
using Konnect.FileSystem;
using Konnect.Management.Text;
using SixLabors.ImageSharp;

namespace Konnect.Management.Batch
{
    public class BatchExtractor(IFileManager fileManager, IProgressContext progress)
    {
        private IList<string>? _options;

        public bool ReuseDialogOptions { get; set; }

        public event Func<BatchFileResult, Task>? FileProcessed;

        public async Task<BatchFileResult[]> Extract(string inputFolder, string? outputFolder, IFilePlugin plugin, BatchOptions options)
        {
            string[] inputFiles = await CollectFiles(inputFolder, plugin, options);
            string[] outputPaths = GetOutputPaths(inputFiles, outputFolder, plugin, options);

            return await Extract(inputFiles, outputPaths, plugin, options);
        }

        public async Task<BatchFileResult[]> Extract(string[] inputFiles, string? outputFolder, IFilePlugin plugin, BatchOptions options)
        {
            string[] outputPaths = GetOutputPaths(inputFiles, outputFolder, plugin, options);

            return await Extract(inputFiles, outputPaths, plugin, options);
        }

        public async Task<BatchFileResult[]> Extract(string[] inputFiles, string[] outputPaths, IFilePlugin plugin, BatchOptions options)
        {
            _options = options.DialogOptions;

            // Fonts currently have no extracted representation
            if (plugin.PluginType is PluginType.Font)
                return [];

            var result = new List<BatchFileResult>();

            progress.StartProgress();

            for (var i = 0; i < inputFiles.Length; i++)
            {
                if (i >= outputPaths.Length)
                    break;

                var fileProgress = progress.CreateScope(i / (double)inputFiles.Length * 100, (i + 1) / (double)inputFiles.Length * 100);
                progress.ReportProgress(i, inputFiles.Length);

                var fileResult = await ExtractFile(inputFiles[i], outputPaths[i], plugin, fileProgress, options);
                result.Add(fileResult);
            }

            progress.ReportProgress(inputFiles.Length, inputFiles.Length);

            progress.FinishProgress();

            return [.. result];
        }

        private async Task<string[]> CollectFiles(string folderPath, IFilePlugin plugin, BatchOptions options)
        {
            SearchOption searchDepth = options.SubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] files = Directory.GetFiles(folderPath, "*", searchDepth);

            if (!plugin.CanIdentifyFiles)
                return files;

            var result = new List<string>();

            foreach (string filePath in files)
            {
                var canIdentify = await fileManager.Identify(filePath, plugin.PluginId);
                if (!canIdentify)
                    continue;

                result.Add(filePath);
            }

            return [.. result];
        }

        private static string[] GetOutputPaths(string[] inputFiles, string? outputFolder, IFilePlugin plugin, BatchOptions options)
        {
            var result = new List<string>(inputFiles.Length);

            foreach (var inputFile in inputFiles)
            {
                switch (plugin.PluginType)
                {
                    case PluginType.Text:
                        result.Add(GetTextOutputPath(inputFile, outputFolder, options.TextOptions));
                        break;

                    case PluginType.Image:
                        result.Add(GetImageOutputPath(inputFile, outputFolder));
                        break;

                    case PluginType.Archive:
                        result.Add(GetArchiveOutputPath(inputFile, outputFolder));
                        break;
                }
            }

            return [.. result];
        }

        private static string GetTextOutputPath(string inputFile, string? outputFolder, BatchTextOptions? options)
        {
            outputFolder ??= Path.GetDirectoryName(Path.GetFullPath(inputFile))!;

            string fileName = Path.GetFileName(inputFile).Replace('.', '_');
            fileName = options?.Format switch
            {
                TextFormat.Kup => fileName + ".kup",
                TextFormat.Po => fileName + ".po",
                _ => fileName
            };

            return Path.Combine(outputFolder, fileName);
        }

        private static string GetImageOutputPath(string inputFile, string? outputFolder)
        {
            outputFolder ??= Path.GetDirectoryName(Path.GetFullPath(inputFile))!;
            string imageFolder = Path.GetFileName(inputFile).Replace('.', '_');

            return Path.Combine(outputFolder, imageFolder);
        }

        private static string GetArchiveOutputPath(string inputFile, string? outputFolder)
        {
            outputFolder ??= Path.GetDirectoryName(Path.GetFullPath(inputFile))!;
            string imageFolder = Path.GetFileName(inputFile).Replace('.', '_');

            return Path.Combine(outputFolder, imageFolder);
        }

        private async Task<BatchFileResult> ExtractFile(string inputFile, string outputPath, IFilePlugin plugin, IProgressContext fileProgress, BatchOptions options)
        {
            BatchFileResult result;

            var loadContext = new LoadFileContext { PluginId = plugin.PluginId };

            if (ReuseDialogOptions && _options is not null)
                loadContext.Options.AddRange(_options);

            var loadResult = await fileManager.LoadFile(inputFile, loadContext);
            if (loadResult.Status is not LoadStatus.Successful || loadResult.LoadedFileState is null)
            {
                result = new BatchFileResult(inputFile, [], loadResult.Reason is LoadErrorReason.NoOptions ? BatchFileStatus.NoOptions : BatchFileStatus.Error);
                await OnFileProcessed(result);

                return result;
            }

            switch (loadResult.LoadedFileState.PluginState)
            {
                case ITextFilePluginState text:
                    await ExtractText(loadResult.LoadedFileState, text, outputPath, options.TextOptions);
                    break;

                case IImageFilePluginState image:
                    await ExtractImage(loadResult.LoadedFileState, image, outputPath, fileProgress);
                    break;

                case IArchiveFilePluginState archive:
                    await ExtractArchive(loadResult.LoadedFileState, archive, outputPath, fileProgress);
                    break;
            }

            result = new BatchFileResult(inputFile, loadResult.LoadedFileState.DialogOptions, BatchFileStatus.Success);
            await OnFileProcessed(result);

            if (ReuseDialogOptions)
                _options = [.. loadResult.LoadedFileState.DialogOptions];

            _ = fileManager.Close(loadResult.LoadedFileState);

            return result;
        }

        private async Task ExtractText(IFileState file, ITextFilePluginState state, string outputFile, BatchTextOptions? options)
        {
            var outputDirectory = Path.GetDirectoryName(outputFile);
            Directory.CreateDirectory(outputDirectory!);

            var previewState = options?.Preview?.CreatePluginState(file.FilePath, fileManager);
            var entries = CreateEntries(state, previewState);

            await using var outputFileStream = File.Create(outputFile);

            switch (options?.Format)
            {
                case TextFormat.Kup:
                    KupManager.Save(outputFileStream, entries);
                    break;

                case TextFormat.Po:
                    PoManager.Save(outputFileStream, entries);
                    break;
            }
        }

        private static TranslationFileEntry[] CreateEntries(ITextFilePluginState state, IGamePluginState? previewState)
        {
            var result = new List<TranslationFileEntry>();

            Dictionary<string, int> entryNameLookup = [];
            Dictionary<string, int> pageNameLookup = [];

            var parser = previewState?.TextProcessing?.Parser ?? new CharacterParser();
            var serializer = previewState?.TextProcessing?.Serializer ?? new CharacterSerializer();

            var pager = state.Pager;
            if (pager is not null)
            {
                var pages = pager.Page(state.Texts);
                for (var i = 0; i < pages.Length; i++)
                {
                    if (pages[i].Entries is null)
                        continue;

                    string pageName = CreatePageName(pages[i], i, pageNameLookup);

                    for (var j = 0; j < pages[i].Entries!.Count; j++)
                    {
                        string entryName = CreateEntryName(pages[i].Entries![j], j, entryNameLookup);
                        string serializedText = serializer.Serialize(parser.Parse(pages[i].Entries![j].TextData, pages[i].Entries![j].Encoding), true);

                        var translatedEntry = new TranslationFileEntry
                        {
                            Name = entryName,
                            PageName = pageName,
                            OriginalText = serializedText,
                            TranslatedText = serializedText
                        };

                        result.Add(translatedEntry);
                    }
                }
            }
            else
            {
                for (var i = 0; i < state.Texts.Count; i++)
                {
                    string entryName = CreateEntryName(state.Texts[i], i, entryNameLookup);
                    string serializedText = serializer.Serialize(parser.Parse(state.Texts[i].TextData, state.Texts[i].Encoding), true);

                    var translatedEntry = new TranslationFileEntry
                    {
                        Name = entryName,
                        OriginalText = serializedText,
                        TranslatedText = serializedText
                    };

                    result.Add(translatedEntry);
                }
            }

            return [.. result];
        }

        private static string CreatePageName(TextEntryPage page, int index, Dictionary<string, int> pageNameLookup)
        {
            if (page.Name is null)
                return $"no_name_{index:00}";

            string pageName = page.Name;

            if (!pageNameLookup.TryGetValue(pageName, out int count))
            {
                pageNameLookup[pageName] = 1;
            }
            else
            {
                pageNameLookup[pageName]++;
                pageName += $"_{count}";
            }

            return pageName;
        }

        private static string CreateEntryName(TextEntry entry, int index, Dictionary<string, int> entryNameLookup)
        {
            if (entry.Name is null)
                return $"no_name_{index:00}";

            string entryName = entry.Name;

            if (!entryNameLookup.TryGetValue(entryName, out int count))
            {
                entryNameLookup[entryName] = 1;
            }
            else
            {
                entryNameLookup[entryName]++;
                entryName += $"_{count}";
            }

            return entryName;
        }

        private static async Task ExtractImage(IFileState file, IImageFilePluginState state, string outputFolder, IProgressContext fileProgress)
        {
            var outputFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(outputFolder, file.StreamManager);

            fileProgress.StartProgress();

            var index = 0;
            foreach (var inputImage in state.Images)
            {
                fileProgress.ReportProgress(index++, state.Images.Count);

                var imageName = string.IsNullOrEmpty(inputImage.ImageInfo.Name) ?
                    file.FilePath.GetNameWithoutExtension() + $".{index - 1:00}" :
                    inputImage.ImageInfo.Name;

                await using var outputFileStream = await outputFileSystem.OpenFileAsync(imageName + ".png", FileMode.Create, FileAccess.Write, FileShare.Write);

                await inputImage.GetImage().SaveAsPngAsync(outputFileStream);
            }

            fileProgress.FinishProgress();
        }

        private static async Task ExtractArchive(IFileState file, IArchiveFilePluginState state, string outputFolder, IProgressContext fileProgress)
        {
            var outputFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(outputFolder, file.StreamManager);

            fileProgress.StartProgress();

            var index = 0;
            foreach (var inputFile in state.Files)
            {
                fileProgress.ReportProgress(index++, state.Files.Count);

                await using var outputFileStream = await outputFileSystem.OpenFileAsync(inputFile.FilePath, FileMode.Create, FileAccess.Write, FileShare.Write);
                var inputFileStream = await inputFile.GetFileData();

                await inputFileStream.CopyToAsync(outputFileStream);
            }

            fileProgress.FinishProgress();
        }

        private async Task OnFileProcessed(BatchFileResult result)
        {
            if (FileProcessed is null)
                return;

            await FileProcessed.Invoke(result);
        }
    }
}
