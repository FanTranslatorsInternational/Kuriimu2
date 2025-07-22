using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_ci_games.Archives
{
    public class Dpk4Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("17A65248-8CD3-4B29-B101-C82FFFCB1D4A");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.dpk"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["IcySon55"],
            Name = "DPK4",
            Publisher = "CI Games",
            Developer = "CI Games",
            Platform = ["PC"],
            LongDescription = "An archive plugin for Project Earth: Starmageddon."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "DPK4";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new Dpk4State();
        }
    }
}
