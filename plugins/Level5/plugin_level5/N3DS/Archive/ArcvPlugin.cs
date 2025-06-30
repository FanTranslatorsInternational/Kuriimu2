using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.N3DS.Archive
{
    public class ArcvPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("db8c2deb-f11d-43c8-bb9e-e271408fd896");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.arc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "ARCV",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["3DS"],
            LongDescription = "Generic archive for 3DS Level-5 games"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "ARCV";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new ArcvState();
        }
    }
}
