using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_square_enix.Archives
{
    public class DpkPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("16951227-46b9-436c-9a02-1016ee6ffda3");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.dpk"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "DPK",
            Publisher = "Square Enix",
            Developer = "Square Enix",
            Platform = ["3DS"],
            LongDescription = "The main resource for Final Fantasy 1 3DS."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new DpkState();
        }
    }
}
