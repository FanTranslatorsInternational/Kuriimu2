using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using Konnect.Extensions;

namespace plugin_spike_chunsoft.Images
{
    public class SrdPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("5fb1ce7a-d657-461b-a282-9659db7337a1");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.srd", "*.srdv"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "SRD",
            Publisher = "Spike Chunsoft",
            Developer = "Spike Chunsoft",
            Platform = ["PC"],
            LongDescription = "The main image resource in Danganronpa 3."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            filePath = filePath.GetExtensionWithDot() == ".srd" ? filePath : filePath.ChangeExtension(".srd");
            if (!fileSystem.FileExists(filePath))
                return false;

            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "$CFH";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new SrdState();
        }
    }
}
