using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_arc_system_works.Images
{
    public class PicaRgPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("49682fbd-3c86-4b40-93f3-8bfc1bbbd53b");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.lzb"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "PicaRg",
            Publisher = "Arc System Works",
            Developer = "Arc System Works",
            Platform = ["3DS"],
            LongDescription = "The main image resource in Jake Hunter by Arc System Works."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(6) == "picaRg";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PicaRgState();
        }
    }
}
