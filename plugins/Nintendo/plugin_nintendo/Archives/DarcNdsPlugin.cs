using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class DarcNdsPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e33b7ba1-5dd3-4afe-b2f3-754e29fc85b1");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.darc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "DARC NDS",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["NDS"],
            LongDescription = "DARC resource archive on NDS systems."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "DARC";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new DarcNdsState();
        }
    }
}
