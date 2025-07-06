using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_ruby_party.Archives
{
    public class CdarPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("01d70a40-c200-4741-9db8-2f22f930b975");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "CDAR",
            Publisher = "Koei Tecmo",
            Developer = "Ruby Party",
            Platform = ["3DS"],
            LongDescription = "The main archive for Attack on Titan: Escape from Certain Death on 3DS."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "CDAR";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new CdarState();
        }
    }
}
