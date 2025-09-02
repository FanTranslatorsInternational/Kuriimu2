using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_powervr
{
    public class PvrPlugin : IDeprecatedFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("9344a849-d176-4a70-8f10-9f4f82d5d03e");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.pvr"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "PVR",
            Publisher = "PowerVR",
            Developer = "PowerVR",
            Platform = ["PC"],
            LongDescription = "The image format of the PowerVR SDK."
        };

        public DeprecatedPluginAlternative[] Alternatives { get; } =
        [
            new() { ToolName = "PowerVR SDK", Url = "https://powervr-graphics.github.io/Native_SDK/Documentation/SDKBrowser/PowerVRTools.html" },
            new() { ToolName = "Kuriimu 1", Url = "https://github.com/IcySon55/Kuriimu" }
        ];

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) is "PVR\x3" or "\x3" + "RVP";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            throw new NotImplementedException();
        }
    }
}
