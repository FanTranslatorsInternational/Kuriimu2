using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_square_enix.Archives
{
    public class SarPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("3c55e976-66aa-4d39-abf9-e48f03d5a624");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.sar"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "SAR",
            Publisher = "Square Enix",
            Developer = "Square Enix",
            Platform = ["NDS"],
            LongDescription = "The Archive resource in Heroes of Mana."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "sar ";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new SarState();
        }
    }
}
