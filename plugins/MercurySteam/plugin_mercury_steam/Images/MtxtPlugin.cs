using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using Konnect.Management.Files;

namespace plugin_mercury_steam.Images
{
    public class MtxtPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("c21befd9-2854-45fb-889f-6e42d374c1f3");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "MTXT",
            Publisher = "MercurySteam",
            Developer = "MercurySteam",
            Platform = ["3DS"],
            LongDescription = "The main image resource in Mercury Steam games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "MTXT";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new MtxtState(pluginFileManager);
        }
    }
}
