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
    public class Nus3Plugin : IDeprecatedFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("3fcd0126-7150-4d0f-9d77-7b781ca0326d");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.nus3bank"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "NUS3",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["3DS"],
            LongDescription = "A common sound archive for Nintendo games."
        };

        public DeprecatedPluginAlternative[] Alternatives { get; } =
        [
            new() { ToolName = "NUS3BANK Editor", Url = "https://github.com/Coolsonickirby/NUS3AUDIO-Editor" },
            new() { ToolName = "Kuriimu 1", Url = "https://github.com/IcySon55/Kuriimu" }
        ];

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) is "NUS3";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            throw new NotImplementedException();
        }
    }
}
