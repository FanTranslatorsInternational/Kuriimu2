using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Images
{
    public class BchPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("acd66c9c-da38-48e5-b614-2f4569775a5e");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.bch"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BCH",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["3DS"],
            LongDescription = "The object resource for games on Nintendo 3DS."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(3) == "BCH";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new BchState();
        }
    }
}
