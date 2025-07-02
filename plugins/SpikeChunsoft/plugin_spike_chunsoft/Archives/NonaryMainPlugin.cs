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
    public class NonaryMainPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f7ca4d58-f7de-0999-87bd-77c8074521a4");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["Neobeo", "onepiecefreak"],
            Name = "Nonary Games",
            Publisher = "Spike Chunsoft",
            Developer = "Spike Chunsoft",
            Platform = ["PC"],
            LongDescription = "The main resource for The Nonary Games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadUInt32() == 0xd7d6a6b8;
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new NonaryMainState();
        }
    }
}
