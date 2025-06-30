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
    public class MtexPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("da18bdbb-a094-4d6f-93ad-c33c7da92881");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.totexk"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["Megaflan"],
            Name = "TOTEXK",
            Publisher = "Bandai Namco",
            Developer = "Bandai Namco",
            Platform = ["3DS"],
            LongDescription = "The image format found in Tales of Abyss 3DS."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.PeekString(4) == "XETM";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new MtexState();
        }
    }
}
