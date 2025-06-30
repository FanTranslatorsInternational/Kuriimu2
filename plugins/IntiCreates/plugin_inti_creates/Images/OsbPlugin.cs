using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_inti_creates.Images
{
    public class OsbPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("2f9faa67-afd0-4209-a2a5-b67974bb9a03");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.osbctr", "*.osb"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "OSB",
            Publisher = "Inti Creates",
            Developer = "Inti Creates",
            Platform = ["3DS"],
            LongDescription = "The main image resource for IntiCreate games."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new OsbState();
        }
    }
}
