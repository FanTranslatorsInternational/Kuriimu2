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
    public class DarcPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f49fda83-44d8-42be-bdba-5c6a787edc11");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.arc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "DARC",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["3DS"],
            LongDescription = "Archive found in Nintendo games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            var magic = br.ReadString(4);
            var magic2 = br.ReadString(4);
            br.BaseStream.Position = 5;
            var magic3 = br.ReadString(4);

            return magic == "darc" || magic2 == "darc" || magic3 == "darc";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new DarcState();
        }
    }
}
