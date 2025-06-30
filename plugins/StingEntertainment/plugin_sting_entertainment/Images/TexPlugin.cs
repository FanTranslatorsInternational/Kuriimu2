using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_sting_entertainment.Images
{
    public class TexPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("dcd694e0-31b5-481c-8cb8-4bec0fc05233");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.tex"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "TEX",
            Publisher = "Atlus",
            Developer = "Sting Entertainment",
            Platform = ["Vita"],
            LongDescription = "The main texture in Dungeon Travelers 2."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(8) == "Texture ";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new TexState();
        }
    }
}
