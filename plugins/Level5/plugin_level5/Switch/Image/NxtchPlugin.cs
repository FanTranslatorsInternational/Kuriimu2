using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.Switch.Image
{
    public class NxtchPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("89222f8f-a345-45ed-9b79-e9e873bda1e9");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.nxtch"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "NXTCH",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["Switch"],
            LongDescription = "The main image resource in some Level5 Switch games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(5) == "NXTCH";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new NxtchState();
        }
    }
}
