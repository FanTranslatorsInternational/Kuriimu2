using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_konami.Archives
{
    public class PackPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("a4615fdf-f408-4d22-a3fe-17f082f974e0");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.pack"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "PACK",
            Publisher = "Konami",
            Developer = "Konami",
            Platform = ["3DS"],
            LongDescription = "The resource archive in New Love Plus."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "PACK";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PackState();
        }
    }
}
