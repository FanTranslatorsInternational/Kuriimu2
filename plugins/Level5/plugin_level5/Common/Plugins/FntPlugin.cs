using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using plugin_level5.Common.Archive.Models;
using plugin_level5.Common.Archive;

namespace plugin_level5.Common.Plugins
{
    public class FntPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("b1b397c4-9a02-4828-b568-39cad733fa3a");

        public PluginType PluginType => PluginType.Font;
        public string[] FileExtensions => ["*.xf"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "FNT",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["PSP", "Vita", "3DS", "Switch"],
            LongDescription = "Font for Level-5 games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            var typeReader = new ArchiveTypeReader();
            if (!typeReader.TryPeek(fileStream, out ArchiveType type))
                return false;

            var readerFactory = new ArchiveReaderFactory();
            IArchiveReader reader = readerFactory.Create(type);

            ArchiveData data = reader.Read(fileStream);
            return data.Files.Any(x => x.Name == "FNT.bin");
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new FntState(pluginFileManager);
        }
    }
}
