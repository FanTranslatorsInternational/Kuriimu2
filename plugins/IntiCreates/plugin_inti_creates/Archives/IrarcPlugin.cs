using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_inti_creates.Archives
{
    public class IrarcPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("9bd9e260-6e91-48cb-9603-2e0c40e06013");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.irarc", "*.irlst"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "IRARC",
            Publisher = "Inti Creates",
            Developer = "Inti Creates",
            Platform = ["3DS"],
            LongDescription = "An archive for Azure Striker Gunvolt on 3DS."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new IrarcState();
        }
    }
}
