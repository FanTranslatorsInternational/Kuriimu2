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
    public class NARCPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("2033a334-3c14-413c-af28-7e1f95f93bd0");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.narc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "NARC",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["NDS"],
            LongDescription = "Standard resource archive on NDS."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "NARC";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new NARCState();
        }
    }
}
