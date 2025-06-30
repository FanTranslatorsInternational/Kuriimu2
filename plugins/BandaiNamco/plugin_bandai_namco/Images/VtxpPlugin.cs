using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_bandai_namco.Images
{
    public class VtxpPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("a10d9fe1-3c86-44f4-9585-454afc432393");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => [".txp"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "VTXP",
            Publisher = "Bandai Namco",
            Developer = "Bandai Namco",
            Platform = ["Vita"],
            LongDescription = "Main image resource for Bandai Namco games on Sony PS Vita."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);

            // Magics for possible compressions
            fileStream.Position++;
            var magic2 = br.ReadString(4);
            var magic3 = br.ReadString(4);

            return magic == "VTXP" || magic2 == "VTXP" || magic3 == "VTXP";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new VtxpState();
        }
    }
}
