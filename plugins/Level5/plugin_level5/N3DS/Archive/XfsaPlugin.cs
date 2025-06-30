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
    public class XfsaPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f712c7ef-1585-48a2-857c-86d0f40054fb");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.fa"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "XFSA",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["3DS"],
            LongDescription = "Main game archive for 3DS Level-5 games"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "XFSA";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new XfsaState();
        }
    }
}
