using Kaligraphy.Parsing;
using Konnect.Contract.DataClasses.FileSystem;
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
using SixLabors.ImageSharp.PixelFormats;

namespace Konnect.Management.Batch
{
    public class BatchInjector(IFileManager fileManager, ISetMaxProgressContext progress)
    {
        private IList<string>? _options;

        public bool ReuseDialogOptions { get; set; }

        public event Func<BatchFileResult, Task>? FileProcessed;

        public async Task<BatchFileResult[]> Inject(string inputFolder, string? outputFolder, IFilePlugin plugin, BatchOptions options)
        {
            (string, string)[] filePaths = await CollectFiles(inputFolder, outputFolder, plugin, options);

            return await Inject(filePaths, plugin, options);
        }

        public async Task<BatchFileResult[]> Inject(string[] inputFiles, string? outputFolder, IFilePlugin plugin, BatchOptions options)
        {
            (string, string)[] filePaths = await CollectFiles(inputFiles, outputFolder, plugin, options);

            return await Inject(filePaths, plugin, options);
        }

        private async Task<BatchFileResult[]> Inject((string, string)[] filePaths, IFilePlugin plugin, BatchOptions options)
        {
            _options = options.DialogOptions;

            // Fonts currently have no extracted representation
            if (plugin.PluginType is PluginType.Font)
                return [];

            var result = new List<BatchFileResult>();

            progress.StartProgress();

            var index = 0;
            foreach ((string inputFile, string outputPath) in filePaths)
            {
                var fileProgress = progress.CreateScope(index / (double)filePaths.Length * 100, (index + 1) / (double)filePaths.Length * 100);
                progress.ReportProgress(index++, filePaths.Length);

                var fileResult = await InjectFile(inputFile, outputPath, plugin, fileProgress, options);
                result.Add(fileResult);
            }

            progress.ReportProgress(filePaths.Length, filePaths.Length);

            progress.FinishProgress();

            return [.. result];
        }

        private async Task<(string, string)[]> CollectFiles(string inputFolder, string? outputFolder, IFilePlugin plugin, BatchOptions options)
        {
            SearchOption searchDepth = options.SubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] inputFiles = Directory.GetFiles(inputFolder, "*", searchDepth);

            return await CollectFiles(inputFiles, outputFolder, plugin, options);
        }

        private async Task<(string, string)[]> CollectFiles(string[] inputFiles, string? outputFolder, IFilePlugin plugin, BatchOptions options)
        {
            var result = new List<(string, string)>();

            foreach (string inputFile in inputFiles)
            {
                string outputPath;
                switch (plugin.PluginType)
                {
                    case PluginType.Archive:
                        outputPath = GetArchiveOutputPath(inputFile, outputFolder);
                        if (!Directory.Exists(outputPath))
                            continue;
                        break;

                    case PluginType.Image:
                        outputPath = GetImageOutputPath(inputFile, outputFolder);
                        if (!Directory.Exists(outputPath))
                            continue;
                        break;

                    case PluginType.Text:
                        outputPath = GetTextOutputPath(inputFile, outputFolder, options.TextOptions);
                        if (!File.Exists(outputPath))
                            continue;
                        break;

                    default:
                        continue;
                }

                if (plugin.CanIdentifyFiles)
                {
                    var canIdentify = await fileManager.Identify(inputFile, plugin.PluginId);
                    if (!canIdentify)
                        continue;
                }

                result.Add((inputFile, outputPath));
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

        private async Task<BatchFileResult> InjectFile(string inputFile, string outputPath, IFilePlugin plugin, IProgressContext fileProgress, BatchOptions options)
        {
            BatchFileResult result;

            var loadContext = new LoadFileContext { PluginId = plugin.PluginId };

            if (ReuseDialogOptions && _options is not null)
                loadContext.Options.AddRange(_options);

            var loadResult = await fileManager.LoadFile(inputFile, loadContext);
            if (loadResult.Status is not LoadStatus.Successful || loadResult.LoadedFileState is null)
            {
                var status = loadResult.Reason is LoadErrorReason.NoOptions ? BatchFileStatus.NoOptions : BatchFileStatus.Error;
                result = new BatchFileResult(inputFile, status, loadResult.DialogFields ?? []);
                await OnFileProcessed(result);

                return result;
            }

            switch (loadResult.LoadedFileState.PluginState)
            {
                case ITextFilePluginState text:
                    await InjectText(loadResult.LoadedFileState, text, outputPath, options.TextOptions);
                    break;

                case IImageFilePluginState image:
                    await InjectImage(loadResult.LoadedFileState, image, outputPath, fileProgress);
                    break;

                case IArchiveFilePluginState archive:
                    await InjectArchive(loadResult.LoadedFileState, archive, outputPath, fileProgress);
                    break;
            }

            _ = await fileManager.SaveFile(loadResult.LoadedFileState);

            result = new BatchFileResult(inputFile, BatchFileStatus.Success, loadResult.LoadedFileState.DialogFields);
            await OnFileProcessed(result);

            if (ReuseDialogOptions)
                _options = [.. loadResult.LoadedFileState.DialogFields.Select(x => x.Result!)];

            _ = fileManager.Close(loadResult.LoadedFileState);

            return result;
        }

        private async Task InjectText(IFileState file, ITextFilePluginState state, string outputFile, BatchTextOptions? options)
        {
            await using var outputFileStream = File.OpenRead(outputFile);

            TranslationFileEntry[] entries;
            switch (options?.Format)
            {
                case TextFormat.Kup:
                    entries = KupManager.Load(outputFileStream);
                    break;

                case TextFormat.Po:
                    entries = PoManager.Load(outputFileStream);
                    break;

                default:
                    return;
            }

            var previewState = options.Preview?.CreatePluginState(file.FilePath, fileManager);

            ImportFileEntries(entries, state, previewState);
        }

        private static void ImportFileEntries(TranslationFileEntry[] entries, ITextFilePluginState state, IGamePluginState? previewState)
        {
            Dictionary<string, int> pageCountLookup = [];
            Dictionary<string, int> entryCountLookup = [];

            var deserializer = previewState?.TextProcessing?.Deserializer ?? new CharacterDeserializer();
            var composer = previewState?.TextProcessing?.Composer ?? new CharacterComposer();

            var pager = state.Pager;
            if (pager is not null)
            {
                var pages = pager.Page(state.Texts);
                for (var i = 0; i < pages.Length; i++)
                {
                    if (pages[i].Entries is null)
                        continue;

                    string pageName = CreatePageName(pages[i], i, pageCountLookup);

                    for (var j = 0; j < pages[i].Entries!.Count; j++)
                    {
                        string entryName = CreateEntryName(pages[i].Entries![j], j, entryCountLookup);

                        var entry = entries.FirstOrDefault(e => e.PageName == pageName && e.Name == entryName);
                        if (entry is null)
                            continue;

                        pages[i].Entries![j].ContentChanged = true;
                        pages[i].Entries![j].TextData = composer.Compose(deserializer.Deserialize(entry.TranslatedText), pages[i].Entries![j].Encoding);
                    }
                }
            }
            else
            {
                for (var i = 0; i < state.Texts.Count; i++)
                {
                    string entryName = CreateEntryName(state.Texts[i], i, entryCountLookup);

                    var entry = entries.FirstOrDefault(e => e.Name == entryName);
                    if (entry is null)
                        continue;

                    state.Texts[i].ContentChanged = true;
                    state.Texts[i].TextData = composer.Compose(deserializer.Deserialize(entry.TranslatedText), state.Texts[i].Encoding);
                }
            }
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

        private static async Task InjectImage(IFileState file, IImageFilePluginState state, string outputFolder, IProgressContext fileProgress)
        {
            var outputFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(outputFolder, file.StreamManager);

            fileProgress.StartProgress();

            var index = 0;
            foreach (var image in state.Images)
            {
                fileProgress.ReportProgress(index++, state.Images.Count);

                var imageName = string.IsNullOrEmpty(image.ImageInfo.Name) ?
                    file.FilePath.GetNameWithoutExtension() + $".{index - 1:00}" :
                    image.ImageInfo.Name;
                imageName += ".png";

                if (!outputFileSystem.FileExists(imageName))
                    continue;

                await using var outputFileStream = await outputFileSystem.OpenFileAsync(imageName);
                image.SetImage(Image.Load<Rgba32>(outputFileStream));
            }

            fileProgress.FinishProgress();
        }

        private static async Task InjectArchive(IFileState file, IArchiveFilePluginState state, string outputFolder, IProgressContext fileProgress)
        {
            var outputFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(outputFolder, file.StreamManager);
            var inputFileSystem = (ArchivePluginFileSystem)FileSystemFactory.CreateArchivePluginFileSystem(file);

            fileProgress.StartProgress();

            var index = 0;
            foreach (var filePath in outputFileSystem.EnumerateAllFiles(UPath.Root))
            {
                fileProgress.ReportProgress(index++, state.Files.Count);

                var fileExists = inputFileSystem.FileExists(filePath);

                if (!fileExists && !state.CanAddFiles)
                    continue;

                var outputFileStream = await outputFileSystem.OpenFileAsync(filePath);
                var entry = (AfiFileEntry)inputFileSystem.GetFileEntry(filePath);

                entry.ArchiveFile.SetFileData(outputFileStream);
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
