using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_cattle_call.Images
{
    public class ChnkPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f41f9666-1a3f-4011-9231-4b27302d54f5");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.tex"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "CHNK",
            Publisher = "Kadokawa",
            Developer = "Cattle Call",
            Platform = ["NDS"],
            LongDescription = "Image resource in Metal Max 3."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "CHNK";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new ChnkState();
        }
    }
}
