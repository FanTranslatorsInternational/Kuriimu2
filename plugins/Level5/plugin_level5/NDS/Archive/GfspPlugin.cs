using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.NDS.Archive
{
    public class GfspPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("0fc27e6a-f61e-426f-93c2-62550646ea89");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.ca", "*.cb"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GFSP",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["NDS"],
            LongDescription = "The main resource archive in Level5 games on DS."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "GFSP";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new GfspState();
        }
    }
}
