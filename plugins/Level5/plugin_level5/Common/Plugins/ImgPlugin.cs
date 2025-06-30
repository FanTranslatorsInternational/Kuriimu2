using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.Common.Plugins
{
    public class ImgPlugin : IIdentifyFiles
    {
        private static readonly string[] Magics =
        [
            "IMGC",
            "IMGV",
            "IMGA",
            "IMGN",
            "IMGP"
        ];

        public Guid PluginId => Guid.Parse("79159dba-3689-448f-8343-167d58a54b2c");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.xi"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "IMGx",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["PSP", "Vita", "3DS", "Switch", "Android"],
            LongDescription = "Main image resource for Level-5 games on multiple platforms."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            var magic = br.ReadString(4);
            return Magics.Contains(magic);
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new ImgState(fileManager);
        }
    }
}
