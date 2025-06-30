using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_grezzo.Archives
{
    public class ZarPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("184e9010-0c35-4ab9-a556-262cbbd2d452");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.zar"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "ZAR",
            Publisher = "Nintendo",
            Developer = "Grezzo",
            Platform = ["3DS"],
            LongDescription = "Main archive type in Zelda: Ocarina of Time."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(3) == "ZAR";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new ZarState();
        }
    }
}
