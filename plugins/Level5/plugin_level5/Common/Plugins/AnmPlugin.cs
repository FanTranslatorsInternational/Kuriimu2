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
    public class AnmPlugin : IDeprecatedFilePlugin, IIdentifyFiles
    {
        private static readonly string[] Magics =
        [
            "ANMC",
            "ANMV",
            "ANMA",
            "ANMN",
            "ANMP"
        ];

        public Guid PluginId => Guid.Parse("25b177e3-9391-4833-8608-571551ab6282");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.bin", "*.xa"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "ANMx",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["PSP", "Vita", "3DS", "Switch", "Android"],
            LongDescription = "Main layout resource for Level-5 games on multiple platforms."
        };

        public DeprecatedPluginAlternative[] Alternatives { get; } =
        [
            new() { ToolName = "Level5 Resource Editor", Url = "https://github.com/onepiecefreak3/Level5RessourceEditor" }
        ];

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            string magic = br.ReadString(4);
            return Magics.Contains(magic);
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            throw new NotImplementedException();
        }
    }
}
