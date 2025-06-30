using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.N3DS.Archive
{
    public class FLPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("1d6586c6-2a42-4899-8a81-f2ed4cb053d3");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "FLBin",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["3DS"],
            LongDescription = "The main archive resource in Fantasy Life"
        };

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new FLState();
        }
    }
}
