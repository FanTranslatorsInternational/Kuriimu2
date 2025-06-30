using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_arc_system_works.Archives
{
    public class DgkpPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("03e56bf8-493d-4a92-885a-0bdf104b258e");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.pac"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "DGKP",
            Publisher = "Arc System Works",
            Developer = "Arc System Works",
            Platform = ["3DS"],
            LongDescription = "A resource archive of Chase: Cold Case Investigations on 3DS."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "DGKP";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new DgkpState();
        }
    }
}
