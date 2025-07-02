using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_sony.Archives
{
    public class Ps2DiscPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("c774f77b-4fe4-4550-9ca0-c8967b99eb78");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.iso"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "PS2Disc",
            Publisher = "Sony",
            Developer = "Sony",
            Platform = ["PS2"],
            LongDescription = "The game disc format for all PS2 games."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new Ps2DiscState();
        }
    }
}
