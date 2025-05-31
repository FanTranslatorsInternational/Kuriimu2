using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Font
{
    public class CfntPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("682ca832-3e29-476d-bc16-f8a9f6105452");

        public PluginType PluginType => PluginType.Font;
        public string[] FileExtensions => ["*.bcfnt"];

        public PluginMetadata Metadata { get; } = new()
        {
            Name = "BCFNT",
            Author = "onepiecefreak",
            LongDescription = "The Nintendo SDK standard font."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            string magic = br.ReadString(4);
            return magic is "CFNT";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new CfntState();
        }
    }
}
