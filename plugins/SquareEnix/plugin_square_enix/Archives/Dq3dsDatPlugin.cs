using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_square_enix.Archives
{
    public class Dq3dsDatPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("3015b1c7-ef92-42a3-89ca-e56af26d9d70");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.dat"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "DQ DAT",
            Publisher = "Square Enix",
            Developer = "Square Enix",
            Platform = ["3DS"],
            LongDescription = "The DAT resource found in Dragon Quest Ports on the 3DS."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new Dq3dsDatState();
        }
    }
}
