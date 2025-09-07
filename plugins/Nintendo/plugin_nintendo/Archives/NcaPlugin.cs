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
    public class NcaPlugin : IDeprecatedFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("3fcd0126-7150-4d0f-9d77-7b781ca0326d");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.nca"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "NCA",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["Switch"],
            LongDescription = "The common content archive for Nintendo Switch."
        };

        public DeprecatedPluginAlternative[] Alternatives { get; } =
        [
            new() { ToolName = "nstool", Url = "https://github.com/jakcron/nstool" }
        ];

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            fileStream.Position = 0x200;

            return br.ReadString(4) is "NCA2" or "NCA3";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            throw new NotImplementedException();
        }
    }
}
