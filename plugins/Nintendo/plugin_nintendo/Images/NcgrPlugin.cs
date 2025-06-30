using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Images
{
    public class NcgrPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("805c26f1-9d54-4116-ac84-2628eec5baa5");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.ncgr"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "NCGR",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["NDS"],
            LongDescription = "Nintendo Color Resource"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "RGCN";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new NcgrState();
        }
    }
}
