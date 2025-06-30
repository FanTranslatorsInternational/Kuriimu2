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
    public class SbPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("daad1871-8a85-4f92-adbd-054ac5a91dc7");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.sb"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "SB",
            Publisher = "Nintendo",
            Developer = "Gamefreak",
            Platform = ["3DS"],
            LongDescription = "Archive found in GARCs."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(2) == "SB";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new SbState();
        }
    }
}
