using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_cavia.Archives
{
    public class Dg2DpkPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("92653036-ff2e-40a3-8827-8e1e298bc86c");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.BIN"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "DPK",
            Publisher = "Cavia",
            Developer = "Square Enix",
            Platform = ["PS2"],
            LongDescription = "The main archive in Drakengard 2."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(3) == "dpk";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new Dg2DpkState();
        }
    }
}
