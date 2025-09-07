using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_shift.Archives
{
    public class ResRdpPlugin : IDeprecatedFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("bc1eefbb-4117-4228-98bc-5b934f0ab3e3");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.res"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "RDP",
            Publisher = "Bandai Namco",
            Developer = "Shift",
            Platform = ["Vita"],
            LongDescription = "The archive format in God Eater: Resurrection."
        };

        public DeprecatedPluginAlternative[] Alternatives { get; } =
        [
            new() { ToolName = "Kuriimu 1", Url = "https://github.com/IcySon55/Kuriimu" }
        ];

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) is "Pres" or "rdp ";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            throw new NotImplementedException();
        }
    }
}
