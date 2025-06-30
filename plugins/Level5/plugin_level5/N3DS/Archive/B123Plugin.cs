using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.N3DS.Archive
{
    public class B123Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("948cde6d-e0e8-4518-a38a-9ba5bf6d4e9e");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.fa"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "B123",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["3DS"],
            LongDescription = "Main game archive for older 3DS Level-5 games"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "B123";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new B123State();
        }
    }
}
