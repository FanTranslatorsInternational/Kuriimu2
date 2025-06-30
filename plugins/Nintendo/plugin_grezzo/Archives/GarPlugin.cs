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
    public class GarPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("bf1e60d4-2613-46d0-a338-b94befabc889");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.gar"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GAR",
            Publisher = "Nintendo",
            Developer = "Grezzo",
            Platform = ["3DS"],
            LongDescription = "Main archive type in Zelda: Majoras Mask."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(3) == "GAR";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new GarState();
        }
    }
}
