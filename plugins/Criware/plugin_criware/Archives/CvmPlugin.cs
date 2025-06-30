using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_criware.Archives
{
    public class CvmPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("6fc77f8b-7811-4820-a1b8-c0708d898652");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.cvm"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "CVM",
            Publisher = "Criware",
            Developer = "Criware",
            Platform = ["PS2"],
            LongDescription = "The main archive resource by Cri Middleware in the PS2 era of games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "CVMH";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new CvmState();
        }
    }
}
