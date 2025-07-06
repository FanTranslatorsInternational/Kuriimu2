using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_capcom.Archives
{
    public class Gk1Plugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("5e7d1d34-4106-4d72-8d69-773b2713ae46");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GK1",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["NDS"],
            LongDescription = "The main resource archive in Gyakuten Kenji 1."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new Gk1State();
        }
    }
}
