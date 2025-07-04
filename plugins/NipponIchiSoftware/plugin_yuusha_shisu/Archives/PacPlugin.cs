using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_yuusha_shisu.Archives
{
    public class PacPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("0066a5a4-1303-4673-bc7f-1742879c3562");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.pac"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["StorMyu"],
            Name = "PAC",
            Publisher = "Nippon Ichi Software",
            Developer = "Nippon Ichi Software",
            Platform = ["Vita"],
            LongDescription = "Death of a Hero"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "ARC\0";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PacState();
        }
    }
}
