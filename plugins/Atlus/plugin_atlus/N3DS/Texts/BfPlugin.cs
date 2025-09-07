using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_atlus.N3DS.Texts
{
    public class BfPlugin : IDeprecatedFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("8452e1a6-1625-44b9-8e77-30086a465f79");

        public PluginType PluginType => PluginType.Text;
        public string[] FileExtensions => ["*.bf"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BF",
            Publisher = "Atlus",
            Developer = "Atlus",
            Platform = ["3DS"],
            LongDescription = "Flow description format used in 3DS Atlus games."
        };

        public DeprecatedPluginAlternative[] Alternatives { get; } =
        [
            new() { ToolName = "Atlus Script Tools", Url = "https://github.com/tge-was-taken/Atlus-Script-Tools" }
        ];

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            fileStream.Position = 8;
            return br.ReadString(4) == "FLW0";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            throw new NotImplementedException();
        }
    }
}
