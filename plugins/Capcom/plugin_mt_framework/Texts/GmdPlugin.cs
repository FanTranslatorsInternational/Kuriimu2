using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_mt_framework.Texts
{
    public class GmdPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("cf57f619-6b61-4bbe-95ec-34370f8ade32");
        public PluginType PluginType => PluginType.Text;
        public string[] FileExtensions => ["*.gmd"];
        public PluginMetadata Metadata => new()
        {
            Author = ["onepiecefreak"],
            Name = "GMD",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["3DS", "Switch", "PC", "PS3", "Android"],
            LongDescription = "Main text resource for the MT Framework by Capcom."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            return GmdSupport.TryGetVersion(fileStream, out _);
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new GmdState();
        }
    }
}
