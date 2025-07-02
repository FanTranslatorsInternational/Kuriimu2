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
    public class SpcPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("aa2db4be-250c-4412-8811-05b9060fd418");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.spc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "SPC",
            Publisher = "Spike Chunsoft",
            Developer = "Spike Chunsoft",
            Platform = ["PC"],
            LongDescription = "The main archive in Danganronpa 3."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "CPS.";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new SpcState();
        }
    }
}
