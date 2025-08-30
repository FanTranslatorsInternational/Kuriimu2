using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_shade.Images
{
    public class ShtxPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("01EB4BEE-8C72-44D2-B6B8-13791DEFA487");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.shtx", "*.btx"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["Obluda"],
            Name = "SHTX",
            Publisher = "Level5",
            Developer = "Shade",
            Platform = ["Wii", "Vita", "PSP"],
            LongDescription = "Images for Shade games"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "SHTX";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new ShtxState();
        }
    }
}
