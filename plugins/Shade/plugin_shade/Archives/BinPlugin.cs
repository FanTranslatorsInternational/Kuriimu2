using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_shade.Archives
{
    public class BinPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("a66defb1-bdf6-4d0a-ac7a-78eb418787ea");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["Obluda", "Alpha"],
            Name = "BIN",
            Publisher = "Level5",
            Developer = "Shade",
            Platform = ["Wii"],
            LongDescription = "Archive in various SHADE games"
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new BinState();
        }
    }
}
