using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_koei_tecmo.Images
{
    public class G1tPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("5e8f5e9d-53da-4777-b15f-41f17355fb44");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.g1t"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "G1T",
            Publisher = "Koei Tecmo",
            Developer = "Koei Tecmo",
            Platform = ["3DS", "Vita", "PC", "Switch"],
            LongDescription = "The main image resource in Gust/KoeiTecmo games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);

            var magic1 = br.ReadString(4);
            var magic2 = br.ReadString(4);

            return magic1 is "GT1G" or "G1TG" && magic2 is "0600" or "0500" or "1600";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new G1tState();
        }
    }
}
