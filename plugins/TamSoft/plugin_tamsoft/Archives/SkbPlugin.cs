using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_tamsoft.Archives
{
    public class SkbPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("d0e36110-815c-45d9-9371-63ca258fc358");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "SKB",
            Publisher = "Tamsoft",
            Developer = "Tamsoft",
            Platform = ["3DS"],
            LongDescription = "The main resource archive used in Senran Kagura Burst on 3DS."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new SkbState();
        }
    }
}
