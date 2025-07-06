using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_capcom.Archives
{
    public class ObbPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("aa9d1923-e658-4d41-9efe-266748b8cc6d");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.obb"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "OBB",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["Android"],
            LongDescription = "The main OBB of Capcom mobile MT Framework games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == ".OBB";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new ObbState();
        }
    }
}
