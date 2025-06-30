using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_beeworks.Archives
{
    public class TD3Plugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("adc5ff0e-9857-4a3e-8ccb-3b79c4b6f5e8");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.dat"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "Touch Detective 3",
            Publisher = "BeeWorks",
            Developer = "BeeWorks",
            Platform = ["3DS"],
            LongDescription = "The main resource archive in Touch Detective 3."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new TD3State();
        }
    }
}
