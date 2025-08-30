using Konnect.Contract.DataClasses.FileSystem.Events;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.Parsing;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Text;
using Konnect.Extensions;
using SixLabors.ImageSharp;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Plugin.Game;
using Konnect.DataClasses.Management.Text;
using Konnect.Management.Text;
using Kuriimu2.Cmd.Models.Batch;

namespace Kuriimu2.Cmd.Batch
{
    class BatchExtractor(IFileManager fileManager, ILogger logger)
        : BatchProcessor(fileManager, logger)
    {
        private readonly object _lock = new();
        private readonly IList<UPath> _openedFiles = [];

        public TextOutput TextOutput { get; set; }

        public IGamePlugin? GamePlugin { get; set; }

        protected override async Task ProcessInternal(IFileSystem sourceFileSystem, UPath filePath, IFileSystem destinationFileSystem)
        {
            Logger.Information("Extract {0}.", filePath.FullName);

            IFileState? loadedFileState;
            lock (_lock)
            {
                _openedFiles.Clear();

                // Load file
                if (SourceFileSystemWatcher is not null)
                    SourceFileSystemWatcher.Opened += SourceFileSystemWatcher_Opened;

                loadedFileState = LoadFile(sourceFileSystem, filePath).Result;

                if (SourceFileSystemWatcher is not null)
                    SourceFileSystemWatcher.Opened -= SourceFileSystemWatcher_Opened;

                // If file could not be loaded successfully
                if (loadedFileState is null)
                    return;

                // If one of the opened files was already batched, stop execution
                if (_openedFiles.Any(IsFileBatched))
                {
                    FileManager.Close(loadedFileState);

                    Logger.Information("{0} is/was already processed.", filePath.FullName);
                    return;
                }

                // Add opened files to batched files
                foreach (UPath openedFile in _openedFiles)
                    AddBatchedFile(openedFile);
            }

            switch (loadedFileState.PluginState)
            {
                case IArchiveFilePluginState archiveState:
                    await ExtractArchive(archiveState, loadedFileState.FilePath, destinationFileSystem, filePath);
                    break;

                case IImageFilePluginState imageState:
                    ExtractImage(imageState, destinationFileSystem, filePath);
                    break;

                case ITextFilePluginState textState:
                    ExtractText(textState, loadedFileState.FilePath, destinationFileSystem, filePath);
                    break;

                default:
                    Logger.Error("{0} is not supported.", filePath.FullName);
                    FileManager.Close(loadedFileState);
                    return;
            }

            FileManager.Close(loadedFileState);

            Logger.Information("Extracted {0}.", filePath.FullName);
        }

        private void SourceFileSystemWatcher_Opened(object? sender, FileOpenedEventArgs e)
        {
            _openedFiles.Add(e.OpenedPath);
        }

        private async Task ExtractArchive(IArchiveFilePluginState archiveState, UPath originalFilepath, IFileSystem destinationFileSystem, UPath filePath)
        {
            if (archiveState.Files.Count > 0)
                CreateDirectory(destinationFileSystem, filePath);

            foreach (IArchiveFile afi in archiveState.Files)
            {
                UPath systemPath = filePath / afi.FilePath.ToRelative();
                Stream? newFileStream = null;

                try
                {
                    newFileStream = await destinationFileSystem.OpenFileAsync(systemPath, FileMode.Create, FileAccess.Write);

                    Stream fileStream = await afi.GetFileData();
                    await fileStream.CopyToAsync(newFileStream);
                }
                catch (Exception e)
                {
                    Logger.Fatal(e, "Unexpected error extracting {0}.", originalFilepath);
                }
                finally
                {
                    newFileStream?.Close();
                }
            }
        }

        private void ExtractImage(IImageFilePluginState imageState, IFileSystem destinationFileSystem, UPath filePath)
        {
            if (imageState.Images.Count > 0)
                CreateDirectory(destinationFileSystem, filePath);

            var index = 0;
            foreach (IImageFile img in imageState.Images)
            {
                Stream fileStream = destinationFileSystem.OpenFile(filePath / (img.ImageInfo.Name ?? $"{index:00}") + ".png", FileMode.Create, FileAccess.Write);
                img.GetImage().SaveAsPng(fileStream);

                fileStream.Close();

                index++;
            }
        }

        private void ExtractText(ITextFilePluginState textState, UPath originalFilepath, IFileSystem destinationFileSystem, UPath filePath)
        {
            List<TranslationFileEntry> entries = [];

            ITextEntryPager? pager = textState.Pager;
            if (pager is not null)
            {
                TextEntryPage[] pages = pager.Page(textState.Texts);
                for (var i = 0; i < pages.Length; i++)
                {
                    string pageName = pages[i].Name ?? $"no_name_{i:00}";

                    for (var j = 0; j < pages[i].Entries.Count; j++)
                    {
                        string entryName = pages[i].Entries[j].Name ?? $"no_name_{j:00}";

                        IGamePluginState? gameState = GamePlugin?.CreatePluginState(originalFilepath, textState.Texts, FileManager);
                        if (gameState is null || !gameState.CanProcessTexts)
                            continue;

                        ICharacterParser parser = gameState.TextProcessing!.Parser ?? new CharacterParser();
                        ICharacterSerializer serializer = gameState.TextProcessing!.Serializer ?? new CharacterSerializer();

                        IList<CharacterData> parsedCharacters = parser.Parse(pages[i].Entries[j].TextData, pages[i].Entries[j].Encoding);
                        string text = serializer.Serialize(parsedCharacters, true);

                        entries.Add(new TranslationFileEntry
                        {
                            Name = entryName,
                            PageName = pageName,
                            OriginalText = text,
                            TranslatedText = text
                        });
                    }
                }
            }
            else
            {
                for (var i = 0; i < textState.Texts.Count; i++)
                {
                    string entryName = textState.Texts[i].Name ?? $"no_name_{i:00}";

                    IGamePluginState? gameState = GamePlugin?.CreatePluginState(originalFilepath, textState.Texts, FileManager);
                    if (gameState is null || !gameState.CanProcessTexts)
                        continue;

                    ICharacterParser parser = gameState.TextProcessing!.Parser ?? new CharacterParser();
                    ICharacterSerializer serializer = gameState.TextProcessing!.Serializer ?? new CharacterSerializer();

                    IList<CharacterData> parsedCharacters = parser.Parse(textState.Texts[i].TextData, textState.Texts[i].Encoding);
                    string text = serializer.Serialize(parsedCharacters, true);

                    entries.Add(new TranslationFileEntry
                    {
                        Name = entryName,
                        PageName = null,
                        OriginalText = text,
                        TranslatedText = text
                    });
                }
            }

            if (entries.Count <= 0)
                return;

            UPath path;
            Stream output;
            switch (TextOutput)
            {
                case TextOutput.Po:
                    path = filePath / originalFilepath.GetName() + ".po";

                    output = destinationFileSystem.OpenFile(path, FileMode.OpenOrCreate); 
                    PoManager.Save(output, [.. entries]);

                    output.Close();
                    break;

                case TextOutput.Kup:
                    path = filePath / originalFilepath.GetName() + ".kup";
                    if (!destinationFileSystem.FileExists(path))
                        return;

                    output = destinationFileSystem.OpenFile(path, FileMode.OpenOrCreate);
                    KupManager.Save(output, [.. entries]);

                    output.Close();
                    break;
            }
        }

        private static void CreateDirectory(IFileSystem fileSystem, UPath path)
        {
            if (!fileSystem.DirectoryExists(path))
                fileSystem.CreateDirectory(path);
        }
    }
}
