using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Images
{
    public class BimgPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("7e60edb3-9a23-4efa-a10b-f113da20d1bc");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.bimg"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BIMG",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["3DS"],
            LongDescription = "The thumbnail format for 3DS movies from the eshop."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new BimgState();
        }
    }
}
