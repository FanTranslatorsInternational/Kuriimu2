using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.Switch.Archive
{
    public class G4pkPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("0964a630-2ca3-4063-8e53-bf7210cbc70e");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.g4pk","*.g4pkm"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "G4PK",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["Switch"],
            LongDescription = "Game archive for Switch Level-5 games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "G4PK";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new G4pkState();
        }
    }
}
