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

        public async Task Inject(string folderPath, IFilePlugin plugin, BatchOptions options)
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

                await InjectFile(filePath, plugin, fileProgress, options);
            }

            progress.ReportProgress(files.Length, files.Length);

            progress.FinishProgress();
        }

        private async Task<string[]> CollectFiles(string folderPath, IFilePlugin plugin, BatchOptions options)
        {
            SearchOption searchDepth = options.SubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] files = Directory.GetFiles(folderPath, "*", searchDepth);

            var result = new List<string>();

            foreach (string filePath in files)
            {
                switch (plugin.PluginType)
                {
                    case PluginType.Archive:
                    case PluginType.Image:
                        string inputFolder = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, Path.GetFileName(filePath).Replace('.', '_'));
                        if (!Directory.Exists(inputFolder))
                            continue;
                        break;

                    case PluginType.Text:
                        string inputFile = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, Path.GetFileName(filePath).Replace('.', '_'));
                        inputFile = options.TextOptions?.Format switch
                        {
                            TextFormat.Kup => inputFile + ".kup",
                            TextFormat.Po => inputFile + ".po",
                            _ => filePath
                        };
                        if (!File.Exists(inputFile))
                            continue;
                        break;
                }

                if (plugin.CanIdentifyFiles)
                {
                    var canIdentify = await fileManager.CanIdentify(filePath, plugin.PluginId);
                    if (!canIdentify)
                        continue;
                }

                result.Add(filePath);
            }

            return [.. result];
        }

        private async Task InjectFile(string filePath, IFilePlugin plugin, IProgressContext fileProgress, BatchOptions options)
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
                    await InjectText(loadResult.LoadedFileState, text, filePath, options.TextOptions);
                    break;

                case IImageFilePluginState image:
                    await InjectImage(loadResult.LoadedFileState, image, filePath, fileProgress);
                    break;

                case IArchiveFilePluginState archive:
                    await InjectArchive(loadResult.LoadedFileState, archive, filePath, fileProgress);
                    break;
            }

            _ = await fileManager.SaveFile(loadResult.LoadedFileState);

            await OnFileProcessed(filePath, loadResult.LoadedFileState.DialogOptions, BatchFileStatus.Success);

            if (ReuseDialogOptions)
                _options = [.. loadResult.LoadedFileState.DialogOptions];

            _ = fileManager.Close(loadResult.LoadedFileState);
        }

        private async Task InjectText(IFileState file, ITextFilePluginState state, string filePath, BatchTextOptions? options)
        {
            string inputFolder = Path.GetDirectoryName(filePath) ?? string.Empty;
            var inputFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(inputFolder, file.StreamManager);

            filePath = Path.GetFileName(filePath).Replace('.', '_');
            filePath = options?.Format switch
            {
                TextFormat.Kup => filePath + ".kup",
                TextFormat.Po => filePath + ".po",
                _ => filePath
            };
            await using var outputFileStream = await inputFileSystem.OpenFileAsync(filePath);

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

            var previewState = options.Preview?.CreatePluginState(file.FilePath, state.Texts, fileManager);

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

        private static async Task InjectImage(IFileState file, IImageFilePluginState state, string filePath, IProgressContext fileProgress)
        {
            string inputFolder = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, Path.GetFileName(filePath).Replace('.', '_'));
            var inputFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(inputFolder, file.StreamManager);

            fileProgress.StartProgress();

            var index = 0;
            foreach (var image in state.Images)
            {
                fileProgress.ReportProgress(index++, state.Images.Count);

                var imageName = string.IsNullOrEmpty(image.ImageInfo.Name) ?
                    file.FilePath.GetNameWithoutExtension() + $".{index - 1:00}" :
                    image.ImageInfo.Name;
                imageName += ".png";

                if (!inputFileSystem.FileExists(imageName))
                    continue;

                await using var inputFileStream = await inputFileSystem.OpenFileAsync(imageName);
                image.SetImage(Image.Load<Rgba32>(inputFileStream));
            }

            fileProgress.FinishProgress();
        }

        private static async Task InjectArchive(IFileState file, IArchiveFilePluginState state, string filePath, IProgressContext fileProgress)
        {
            string inputFolder = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, Path.GetFileName(filePath).Replace('.', '_'));
            var inputFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(inputFolder, file.StreamManager);
            var outputFileSystem = (ArchivePluginFileSystem)FileSystemFactory.CreateArchivePluginFileSystem(file);

            fileProgress.StartProgress();

            var index = 0;
            foreach (var inputFilePath in inputFileSystem.EnumerateAllFiles(UPath.Root))
            {
                fileProgress.ReportProgress(index++, state.Files.Count);

                var fileExists = outputFileSystem.FileExists(inputFilePath);

                if (!fileExists && !state.CanAddFiles)
                    continue;

                var inputFileStream = await inputFileSystem.OpenFileAsync(inputFilePath);
                var entry = (AfiFileEntry)outputFileSystem.GetFileEntry(inputFilePath);

                entry.ArchiveFile.SetFileData(inputFileStream);
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
