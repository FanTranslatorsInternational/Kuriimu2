using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_hunex.Archives
{
    public class HEDPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("3102046d-562a-4d81-ae60-828e3ee10e21");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.hed"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["Sn0wCrack", "onepiecefreak"],
            Name = "HED",
            Publisher = "HuneX",
            Developer = "HuneX",
            Platform = ["Vita"],
            LongDescription = "The first main archive for HuneX games."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new HEDState();
        }
    }
}
