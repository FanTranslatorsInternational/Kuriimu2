using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_primula.Archives
{
    public class Pac2Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("AF5ADDBD-BF3A-4168-A287-BD78C9306DEB");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.dat"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["Megaflan"],
            Name = "Pac2",
            Publisher = "Primula",
            Developer = "Primula",
            Platform = ["PC"],
            LongDescription = "The main archive resource in Primula games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(12) == "GAMEDAT PAC2";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new Pac2State();
        }
    }
}
