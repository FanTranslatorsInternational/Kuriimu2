using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_atlus.N3DS.Image
{
    public class StexPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("8075AB6F-5D1F-4EE3-AF90-DDBF1E0852C0");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.stex"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "STEX",
            Publisher = "Atlus",
            Developer = "Atlus",
            Platform = ["3DS"],
            LongDescription = "Image format found in Shin Megami Tensei and Etrian Odyssey 3DS games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "STEX";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new StexState();
        }
    }
}
