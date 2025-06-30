using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.NDS.Image
{
    public class LimgPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("169acf3f-ccc8-4193-b32c-84b44c0f6f68");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.cimg"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "LIMG",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["NDS"],
            LongDescription = "Main image for later DS Level-5 games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "LIMG";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new LimgState();
        }
    }
}
