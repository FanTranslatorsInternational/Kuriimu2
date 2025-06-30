using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_hunex.Archives
{
    public class MRGPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("a2f60c9b-5c70-4415-80c3-50c967ae4ebb");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.mrg", "*.mzp"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["Sn0wcrack", "onepiecefreak"],
            Name = "MRG",
            Publisher = "HuneX",
            Developer = "HuneX",
            Platform = ["Vita"],
            LongDescription = "The second main archive of HuneX games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(6) == "mrgd00";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new MRGState();
        }
    }
}
