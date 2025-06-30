using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class MMBinPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("3f6edc1c-215f-4c25-9e06-1bea714e72fe");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["IcySon55"],
            Name = "MMBin",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["WiiU"],
            LongDescription = "2D resource from Mario Maker."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new MMBinState();
        }
    }
}
