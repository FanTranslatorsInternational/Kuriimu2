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
    public class NstpPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("40f66321-eb99-401e-b510-a2a402741f00");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => [".txp"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "NSTP",
            Publisher = "Bandai Namco",
            Developer = "Bandai Namco",
            Platform = ["Switch"],
            LongDescription = "Main image resource for Bandai Namco games on Nintendo Switch."
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

            return magic == "NSTP" || magic2 == "NSTP" || magic3 == "NSTP";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new NstpState();
        }
    }
}
