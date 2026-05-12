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

        public async Task Extract(string folderPath, IFilePlugin plugin, BatchOptions options)
        {
            // Fonts currently have no extracted representation
            if (plugin.PluginType is PluginType.Font)
                return;

            string[] files = await CollectFiles(folderPath, plugin, options);

            progress.StartProgress();

            var index = 0;
            foreach (string filePath in files)
            {
                var fileProgress = progress.CreateScope(index / (double)files.Length * 100, (index + 1) / (double)files.Length * 100);
                progress.ReportProgress(index++, files.Length);

                await ExtractFile(filePath, plugin, fileProgress, options);
            }

            progress.ReportProgress(files.Length, files.Length);

            progress.FinishProgress();
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
                var canIdentify = await fileManager.CanIdentify(filePath, plugin.PluginId);
                if (!canIdentify)
                    continue;

                result.Add(filePath);
            }

            return [.. result];
        }

        private async Task ExtractFile(string filePath, IFilePlugin plugin, IProgressContext fileProgress, BatchOptions options)
        {
            var loadContext = new LoadFileContext { PluginId = plugin.PluginId };

            if (ReuseDialogOptions && _options is not null)
                loadContext.Options.AddRange(_options);

            var loadResult = await fileManager.LoadFile(filePath, loadContext);
            if (loadResult.Status is not LoadStatus.Successful || loadResult.LoadedFileState is null)
            {
                await OnFileProcessed(filePath, [], BatchFileStatus.Error);
                return;
            }

            switch (loadResult.LoadedFileState.PluginState)
            {
                case ITextFilePluginState text:
                    await ExtractText(loadResult.LoadedFileState, text, filePath, options.TextOptions);
                    break;

                case IImageFilePluginState image:
                    await ExtractImage(loadResult.LoadedFileState, image, filePath, fileProgress);
                    break;

                case IArchiveFilePluginState archive:
                    await ExtractArchive(loadResult.LoadedFileState, archive, filePath, fileProgress);
                    break;
            }

            _ = fileManager.Close(loadResult.LoadedFileState);

            await OnFileProcessed(filePath, loadResult.LoadedFileState.DialogOptions, BatchFileStatus.Success);

            if (ReuseDialogOptions)
                _options = loadResult.LoadedFileState.DialogOptions;
        }

        private async Task ExtractText(IFileState file, ITextFilePluginState state, string filePath, BatchTextOptions? options)
        {
            string outputFolder = Path.GetDirectoryName(filePath) ?? string.Empty;
            var outputFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(outputFolder, file.StreamManager);

            var previewState = options?.Preview?.CreatePluginState(file.FilePath, state.Texts, fileManager);
            var entries = CreateEntries(state, previewState);

            filePath = Path.GetFileName(filePath).Replace('.', '_');
            filePath = options?.Format switch
            {
                TextFormat.Kup => filePath + ".kup",
                TextFormat.Po => filePath + ".po",
                _ => filePath
            };
            await using var outputFileStream = await outputFileSystem.OpenFileAsync(filePath, FileMode.Create, FileAccess.Write, FileShare.Write);

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

        private static async Task ExtractImage(IFileState file, IImageFilePluginState state, string filePath, IProgressContext fileProgress)
        {
            string outputFolder = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, Path.GetFileName(filePath).Replace('.', '_'));
            Directory.CreateDirectory(outputFolder);

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

        private static async Task ExtractArchive(IFileState file, IArchiveFilePluginState state, string filePath, IProgressContext fileProgress)
        {
            string outputFolder = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, Path.GetFileName(filePath).Replace('.', '_'));
            Directory.CreateDirectory(outputFolder);

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

        private async Task OnFileProcessed(string filePath, IList<string> dialogOptions, BatchFileStatus status)
        {
            if (FileProcessed is null)
                return;

            await FileProcessed.Invoke(new BatchFileResult(filePath, dialogOptions, status));
        }
    }
}
