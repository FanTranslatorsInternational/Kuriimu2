using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class UMSBTPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("2546d1de-7ba9-4a1b-a809-247314c57ab5");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.umsbt"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["IcySon55", "onepiecefreak"],
            Name = "UMSBT",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["3DS"],
            LongDescription = "The UMSBT resource for Nintendo games."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new UMSBTState();
        }
    }
}
