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
    public class Garc4Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("fa49a481-8673-4360-beb5-ccd34961df1b");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.garc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GARC v4",
            Publisher = "Nintendo",
            Developer = "Gamefreak",
            Platform = ["3DS"],
            LongDescription = "One kind of archive in Pokemon games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            var magic = br.ReadString(4);
            br.BaseStream.Position = 0xB;
            return magic == "CRAG" && br.ReadByte() == 4;
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new Garc4State();
        }
    }
}
