using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_sony.Images
{
    public class GxtPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("b7453fd6-ca66-4684-b172-8f51db77ea75");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.gxt", "*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak", "IcySon55"],
            Name = "Gxt",
            Publisher = "Sony",
            Developer = "Sony",
            Platform = ["Vita"],
            LongDescription = "The main image resource by the Sony Vita SDK."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.PeekString(4) == "GXT\0";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new GxtState();
        }
    }
}
