using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_mcdonalds.Images
{
    // HINT: Not public due to not being reproducable
    class ECDPNcgrPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("805c26f1-9d54-ecd5-ac84-2628eec5baa5");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.ncgr"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onpiecefreak"],
            Name = "eCDP NCGR",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["NDS"],
            LongDescription = "NCGR's found in eCDP by McDonald's."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "RGCN";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new ECDPNcgrState();
        }
    }
}
