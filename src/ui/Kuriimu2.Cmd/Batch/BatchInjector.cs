using System.Collections.Generic;
using System.IO;
using System.Linq;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Serilog;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading.Tasks;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.Parsing;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Text;
using Konnect.Contract.Plugin.Game;
using Konnect.DataClasses.Management.Text;
using Konnect.Extensions;
using Konnect.Management.Text;
using Kuriimu2.Cmd.Models.Batch;
using SixLabors.ImageSharp;

namespace Kuriimu2.Cmd.Batch
{
    class BatchInjector(IFileManager fileManager, ILogger logger) : BatchProcessor(fileManager, logger)
    {
        public TextOutput TextOutput { get; set; }

        public IGamePlugin? GamePlugin { get; set; }

        protected override async Task ProcessInternal(IFileSystem sourceFileSystem, UPath filePath, IFileSystem destinationFileSystem)
        {
            if (!destinationFileSystem.DirectoryExists(filePath))
                return;

            // Load file
            IFileState? loadedFile = await LoadFile(sourceFileSystem, filePath);
            if (loadedFile is null)
                return;

            switch (loadedFile.PluginState)
            {
                case IArchiveFilePluginState archiveState:
                    InjectArchive(archiveState, destinationFileSystem, filePath);
                    break;

                case IImageFilePluginState imageState:
                    InjectImage(imageState, destinationFileSystem, filePath);
                    break;

                case ITextFilePluginState textState:
                    InjectText(textState, loadedFile.FilePath, destinationFileSystem, filePath);
                    break;

                default:
                    Logger.Error("{0} is not supported.", filePath.FullName);
                    return;
            }

            // Save file with all changes
            await SaveFile(loadedFile);

            // Close file
            FileManager.Close(loadedFile);
        }

        private static void InjectArchive(IArchiveFilePluginState archiveState, IFileSystem destinationFileSystem, UPath filePath)
        {
            foreach (IArchiveFile afi in archiveState.Files)
            {
                UPath path = filePath / afi.FilePath.ToRelative();
                if (!destinationFileSystem.FileExists(path))
                    continue;

                Stream afiFileStream = destinationFileSystem.OpenFile(path);
                afi.SetFileData(afiFileStream);
            }
        }

        private static void InjectImage(IImageFilePluginState imageState, IFileSystem destinationFileSystem, UPath filePath)
        {
            for (var i = 0; i < imageState.Images.Count; i++)
            {
                IImageFile img = imageState.Images[i];

                UPath path = filePath / (img.ImageInfo.Name ?? $"{i:00}") + ".png";
                if (!destinationFileSystem.FileExists(path))
                    continue;

                Image<Rgba32> openedImage = Image.Load<Rgba32>(path.FullName);
                img.SetImage(openedImage);
            }
        }

        private void InjectText(ITextFilePluginState textState, UPath originalFilepath, IFileSystem destinationFileSystem, UPath filePath)
        {
            TranslationFileEntry[] entries;

            UPath path;
            Stream input;
            switch (TextOutput)
            {
                case TextOutput.Po:
                    path = filePath / originalFilepath.GetName() + ".po";
                    if (!destinationFileSystem.FileExists(path))
                        return;

                    input = destinationFileSystem.OpenFile(path);
                    entries = PoManager.Load(input);

                    input.Close();
                    break;

                case TextOutput.Kup:
                    path = filePath / originalFilepath.GetName() + ".kup";
                    if (!destinationFileSystem.FileExists(path))
                        return;

                    input = destinationFileSystem.OpenFile(path);
                    entries = KupManager.Load(input);

                    input.Close();
                    break;

                default:
                    return;
            }

            if (entries.Length <= 0)
                return;

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

                        TranslationFileEntry? fileEntry = entries.FirstOrDefault(x => x.PageName == pageName && x.Name == entryName);
                        if (fileEntry is null)
                            continue;

                        IGamePluginState? gameState = GamePlugin?.CreatePluginState(originalFilepath, pages[i].Entries.AsReadOnly(), FileManager);
                        if (gameState is null || !gameState.CanProcessTexts)
                            continue;

                        ICharacterDeserializer deserializer = gameState.TextProcessing!.Deserializer ?? new CharacterDeserializer();
                        ICharacterComposer composer = gameState.TextProcessing!.Composer ?? new CharacterComposer();

                        IList<CharacterData> deserializedCharacters = deserializer.Deserialize(fileEntry.TranslatedText);
                        byte[] textData = composer.Compose(deserializedCharacters, pages[i].Entries[j].Encoding);

                        pages[i].Entries[j].TextData = textData;
                        pages[i].Entries[j].ContentChanged = true;
                    }
                }
            }
            else
            {
                for (var i = 0; i < textState.Texts.Count; i++)
                {
                    string entryName = textState.Texts[i].Name ?? $"no_name_{i:00}";

                    TranslationFileEntry? fileEntry = entries.FirstOrDefault(x => x.PageName is null && x.Name == entryName);
                    if (fileEntry is null)
                        continue;

                    IGamePluginState? gameState = GamePlugin?.CreatePluginState(originalFilepath, textState.Texts, FileManager);
                    if (gameState is null || !gameState.CanProcessTexts)
                        continue;

                    ICharacterDeserializer deserializer = gameState.TextProcessing!.Deserializer ?? new CharacterDeserializer();
                    ICharacterComposer composer = gameState.TextProcessing!.Composer ?? new CharacterComposer();

                    IList<CharacterData> deserializedCharacters = deserializer.Deserialize(fileEntry.TranslatedText);
                    byte[] textData = composer.Compose(deserializedCharacters, textState.Texts[i].Encoding);

                    textState.Texts[i].TextData = textData;
                    textState.Texts[i].ContentChanged = true;
                }
            }
        }
    }
}
