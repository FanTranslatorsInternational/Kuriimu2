using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_konami.Archives
{
    public class NlpPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("839e2182-87f5-47cd-adac-49c0b61113ff");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "NLP",
            Publisher = "Konami",
            Developer = "Konami",
            Platform = ["3DS"],
            LongDescription = "The main resource for New Love Plus."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new NlpState();
        }
    }
}
