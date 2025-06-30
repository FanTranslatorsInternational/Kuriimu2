using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_ganbarion.Images
{
    public class JtexPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("4fa038e1-bcb8-470b-998c-7f6a4ffa20fa");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.jtex"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "JTEX",
            Publisher = "Bandai Namco",
            Developer = "Ganbarion",
            Platform = ["3DS"],
            LongDescription = "The main image format in ganbarion games on the 3DS."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "jIMG";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new JtexState();
        }
    }
}
