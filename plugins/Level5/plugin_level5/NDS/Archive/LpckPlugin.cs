using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.NDS.Archive
{
    public class LpckPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e28e9665-a30c-46ef-92c2-f5d898bb279e");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.pcm"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "LPCK",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["NDS"],
            LongDescription = "Main resource archive in Professor Layton 1."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            br.BaseStream.Position = 0xC;
            return br.ReadString(4) == "LPCK";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new LpckState();
        }
    }
}
