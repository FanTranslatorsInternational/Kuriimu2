using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class CwarPlugin : IDeprecatedFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("8791e9a6-6574-438d-b8e6-a728c5742bd2");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bcwar"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "CWAR",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["3DS"],
            LongDescription = "The common sound archive for many 3DS games."
        };

        public DeprecatedPluginAlternative[] Alternatives { get; } =
        [
            new() { ToolName = "Kuriimu 1", Url = "https://github.com/IcySon55/Kuriimu" }
        ];

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) is "CWAR";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            throw new NotImplementedException();
        }
    }
}
