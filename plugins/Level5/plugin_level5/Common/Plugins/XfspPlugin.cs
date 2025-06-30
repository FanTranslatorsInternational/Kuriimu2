using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using plugin_level5.Common.Archive;
using plugin_level5.Common.Archive.Models;

namespace plugin_level5.Common.Plugins
{
    public class XfspPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("9f7b5c2d-bea3-4108-97b8-298db97b9c3a");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.xr", "*.xc", "*.xa", "*.xk", "*.xy"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "XFSP",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["PSP", "Vita", "3DS", "Switch"],
            LongDescription = "Main archive for Level-5 games"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            var typeReader = new ArchiveTypeReader();
            if (!typeReader.TryPeek(fileStream, out ArchiveType type) || type is not ArchiveType.Xfsp)
                return false;

            var readerFactory = new ArchiveReaderFactory();
            IArchiveReader reader = readerFactory.Create(type);

            ArchiveData data = reader.Read(fileStream);
            return data.Files.All(x => x.Name != "FNT.bin");
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new ArchiveState();
        }
    }
}
