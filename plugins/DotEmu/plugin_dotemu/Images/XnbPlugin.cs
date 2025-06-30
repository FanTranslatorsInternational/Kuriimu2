using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_dotemu.Images
{
    public class XnbPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("08239e71-2ef6-4e88-b0e9-fbc52116ced2");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.xnb"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "XNB",
            Publisher = "DotEmu",
            Developer = "DotEmu",
            Platform = ["Switch"],
            LongDescription = "Main image resource for Microsoft.XNA.Framework"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(3) == "XNB";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new XnbState();
        }
    }
}