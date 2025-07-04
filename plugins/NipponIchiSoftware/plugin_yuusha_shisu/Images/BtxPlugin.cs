using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_yuusha_shisu.Images
{
    public class BtxPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("df2a52a8-9cbe-4959-a593-ad62ae687c17");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.btx"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["IcySon55","onepiecefreak"],
            Name = "BTX",
            Publisher = "Nippon Ichi Software",
            Developer = "Nippon Ichi Software",
            Platform = ["Vita"],
            LongDescription = "The image resource for Death of a Hero"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "btx\0";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new BtxState();
        }
    }
}
