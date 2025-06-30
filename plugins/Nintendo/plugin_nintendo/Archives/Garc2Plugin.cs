using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class Garc2Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("379f0519-a3c9-4248-9264-0e53d8b6b023");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.garc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GARC v2",
            Publisher = "Nintendo",
            Developer = "Gamefreak",
            Platform = ["NDS"],
            LongDescription = "One kind of archive in Pokemon games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            var magic = br.ReadString(4);
            br.BaseStream.Position = 0xB;
            return magic == "CRAG" && br.ReadByte() == 2;
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new Garc2State();
        }
    }
}
