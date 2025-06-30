using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Images
{
    public class GcBnrPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("50ec8196-b1ff-4d2d-85e8-f56e557ba9c2");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.bnr", "*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BNR",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["GC"],
            LongDescription = "The GameCube Banner format."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            string magic = br.ReadString(4);
            return magic is "BNR1" or "BNR2";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new GcBnrState();
        }
    }
}
