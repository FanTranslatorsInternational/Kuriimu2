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
    public class CpkPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("63909918-ac30-41bb-803b-cee5110c573d");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.cpk"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["IcySon55", "onepiecefreak"],
            Name = "CPK",
            Publisher = "Criware",
            Developer = "Criware",
            Platform = ["3DS", "Vita", "PSP", "Switch"],
            LongDescription = "The main archive for the CriWare Middleware."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "CPK ";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new CpkState();
        }
    }
}
