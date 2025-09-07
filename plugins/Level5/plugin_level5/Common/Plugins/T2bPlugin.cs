using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.Common.Plugins
{
    public class T2bPlugin : IDeprecatedFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("597c7bde-7c63-4845-aad7-289e17b8fd9d");

        public PluginType PluginType => PluginType.Text;
        public string[] FileExtensions => ["*.cfg.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "CFG.BIN",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["PSP", "Vita", "3DS", "Switch"],
            LongDescription = "Main configuration resource in Level-5 games."
        };

        public DeprecatedPluginAlternative[] Alternatives { get; } =
        [
            new() { ToolName = "CfgBinEditor", Url = "https://github.com/onepiecefreak3/CfgBinEditor" }
        ];

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            fileStream.Position = fileStream.Length - 0x10;
            return br.ReadString(4) is "\x1" + "t2b";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            throw new NotImplementedException();
        }
    }
}
