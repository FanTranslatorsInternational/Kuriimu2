using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_ganbarion.Archives
{
    public class JarcPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("26dad045-388d-42f3-a625-ec44dbf2060d");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.jarc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "JARC",
            Publisher = "Bandai Namco",
            Developer = "Ganbarion",
            Platform = ["3DS"],
            LongDescription = "The main archive resource in Ganbarion games on 3DS."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            string magic = br.ReadString(4);

            return magic is "jARC" or "jCMP";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new JarcState();
        }
    }
}
