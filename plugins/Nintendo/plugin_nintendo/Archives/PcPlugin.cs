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
    public class PcPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("50d54c18-cb15-49fc-b002-1210126f502f");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.pc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Name = "PC",
            Author = "onepiecefreak",
            LongDescription = "Archive found in GARCs."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(2) == "PC";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PcState();
        }
    }
}
