using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.NDS.Archive
{
    public class Lpc2Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("d139ebf0-cba1-4338-b688-d7ed49cad392");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.cani"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "LPC2",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["NDS"],
            LongDescription = "Archive in Level-5 DS games"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "LPC2";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new Lpc2State();
        }
    }
}
