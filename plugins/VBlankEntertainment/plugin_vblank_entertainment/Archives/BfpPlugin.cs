using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_vblank_entertainment.Archives
{
    public class BfpPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("2222afb1-c37b-44fc-86df-919fc4093ee4");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bfp"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BFP",
            Publisher = "VBlank Entertainment",
            Developer = "VBlank Entertainment",
            Platform = ["3DS"],
            LongDescription = "Main archive from Retro City Rampage DX on 3DS."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "RTFP";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new BfpState();
        }
    }
}
