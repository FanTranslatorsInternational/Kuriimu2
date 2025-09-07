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
    public class BmdPlugin : IDeprecatedFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f7faf98b-f969-4236-91b9-31c28b70e6a0");

        public PluginType PluginType => PluginType.Text;
        public string[] FileExtensions => ["*.bmd"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BMD",
            Publisher = "Atlus",
            Developer = "Atlus",
            Platform = ["3DS"],
            LongDescription = "Message format used in 3DS Atlus games along with flow descriptions."
        };

        public DeprecatedPluginAlternative[] Alternatives { get; } =
        [
            new() { ToolName = "Atlus Script Tools", Url = "https://github.com/tge-was-taken/Atlus-Script-Tools" }
        ];

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "MLTB";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            throw new NotImplementedException();
        }
    }
}
