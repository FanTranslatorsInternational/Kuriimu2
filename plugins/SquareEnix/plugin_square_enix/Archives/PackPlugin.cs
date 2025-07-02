using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_square_enix.Archives
{
    public class PackPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("00d5ca3f-419f-4426-bea3-168159fe28db");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.pack"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "PACK",
            Publisher = "Square Enix",
            Developer = "Square Enix",
            Platform = ["3DS"],
            LongDescription = "The resource archive for Dragon Quest XI."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            var magic = br.ReadString(4);
            return magic is "PACK" or "PACA";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PackState();
        }
    }
}
