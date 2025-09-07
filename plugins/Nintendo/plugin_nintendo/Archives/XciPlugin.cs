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
    public class XciPlugin : IDeprecatedFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("382bdd4a-e49a-4b47-9aef-3d6d3bf3435e");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.xci"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "XCI",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["Switch"],
            LongDescription = "The common cartridge format for Nintendo Switch."
        };

        public DeprecatedPluginAlternative[] Alternatives { get; } =
        [
            new() { ToolName = "nstool", Url = "https://github.com/jakcron/nstool" }
        ];

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            fileStream.Position = 0x100;

            return br.ReadString(4) is "HEAD";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            throw new NotImplementedException();
        }
    }
}
