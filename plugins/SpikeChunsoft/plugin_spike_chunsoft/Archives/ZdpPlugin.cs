using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_spike_chunsoft.Archives
{
    public class ZdpPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("577d64ef-fe0d-4a17-a9e9-86f75041a392");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.zdp"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "ZDP",
            Publisher = "Spike Chunsoft",
            Developer = "Spike Chunsoft",
            Platform = ["3DS"],
            LongDescription = "The datapack for Spike Chunsoft games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(8) == "datapack";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new ZdpState();
        }
    }
}
