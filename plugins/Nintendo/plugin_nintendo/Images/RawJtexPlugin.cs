using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Images
{
    public class RawJtexPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("dcac8fbe-6911-43ac-a7df-cda5485743e3");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.jtex"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "RawJTEX",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["3DS"],
            LongDescription = "The image format used in 3DS games."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new RawJtexState();
        }
    }
}
