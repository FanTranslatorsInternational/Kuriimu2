using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.NDS.Archive
{
    public class GfsaPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f38e0ef3-f6ad-42d8-bb52-a1d3323d5372");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.fa"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GFSA",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["NDS"],
            LongDescription = "Main resource archive in Professor Layton 4."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "GFSA";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new GfsaState();
        }
    }
}
