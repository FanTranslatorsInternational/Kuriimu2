using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_sega.Images
{
    public class HtxPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("9abf1cd7-be79-43b1-b99b-63bead36aaf0");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.HTX"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "HTEX",
            Publisher = "Sega",
            Developer = "Sega",
            Platform = ["PS2"],
            LongDescription = "The main image resource in Sakura Wars V and games from the same developer."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "HTEX";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new HtexState();
        }
    }
}
