using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_rpg_maker.Archives
{
    public class RgsSadPlugin : IDeprecatedFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("3f6782fe-c24a-45c8-a313-6af6ad77b219");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.rgssad"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "RPG Maker RGASSAD",
            Publisher = "Enterbrain",
            Developer = "Enterbrain",
            Platform = ["NDS"],
            LongDescription = "The commoon encrytped archive in RPG Maker games.."
        };

        public DeprecatedPluginAlternative[] Alternatives { get; } =
        [
            new() { ToolName = "Kuriimu 1", Url = "https://github.com/IcySon55/Kuriimu" }
        ];

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(6) is "RGSSAD";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            throw new NotImplementedException();
        }
    }
}
