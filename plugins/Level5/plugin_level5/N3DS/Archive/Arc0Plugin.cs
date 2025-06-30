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
    public class Arc0Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e75ba21c-f0f4-4d0e-8989-103ea2ac3cda");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.fa"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "ARC0",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["3DS"],
            LongDescription = "Main game archive for 3DS Level-5 games"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "ARC0";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new Arc0State();
        }
    }
}
