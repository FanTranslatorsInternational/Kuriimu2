using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_bandai_namco.Images
{
    public class TotxPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("d05e8cb7-cb50-41b6-9513-494e25989915");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.ttx"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "TOTX",
            Publisher = "Bandai Namco",
            Developer = "Bandai Namco",
            Platform = ["3DS"],
            LongDescription = "Image resource found in FileArc.bin's of Dragon Ball Heroes games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "TOTX";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new TotxState(pluginFileManager);
        }
    }
}
