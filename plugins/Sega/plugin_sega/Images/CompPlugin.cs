using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_sega.Images
{
    public class CompPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("7de736f9-906e-4d1f-823c-b4f189885b6e");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.comp"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "COMP",
            Publisher = "Sega",
            Developer = "Sega",
            Platform = ["3DS"],
            LongDescription = "The image resource found Sega games from the 3DS."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new CompState();
        }
    }
}
