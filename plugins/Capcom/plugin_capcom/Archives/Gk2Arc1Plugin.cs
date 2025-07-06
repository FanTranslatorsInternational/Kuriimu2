using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_capcom.Archives
{
    public class Gk2Arc1Plugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("fdfbae91-a06d-4443-b1d8-cbb1d84797a1");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GK2_1",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["NDS"],
            LongDescription = "The main resource archive for Gyakuten Kenji 2."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new Gk2Arc1State();
        }
    }
}
