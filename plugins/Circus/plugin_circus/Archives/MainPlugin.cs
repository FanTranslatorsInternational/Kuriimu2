using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_circus.Archives
{
    public class MainPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("fa182181-76f5-4b7c-aebf-c8466c01aa1e");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "Da Capo Script",
            Publisher = "Circus",
            Developer = "Circus",
            Platform = ["Vita"],
            LongDescription = "The script file of Da Capo games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            string magic = br.ReadString(3);

            return magic is "DC1" or "DC2";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new MainState();
        }
    }
}
