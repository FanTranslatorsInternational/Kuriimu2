using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_shade.Archives
{
    public class BlnSubPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("6d71d07c-b517-496b-b659-3498cd3542fd");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BLN Sub",
            Publisher = "Level5",
            Developer = "Shade",
            Platform = ["Wii"],
            LongDescription = "Archive in Inazuma Eleven GO Strikers 2013 BLN files."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new BlnSubState();
        }
    }
}
