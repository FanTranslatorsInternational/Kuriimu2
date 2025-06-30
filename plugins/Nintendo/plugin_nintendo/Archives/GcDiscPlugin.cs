using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class GcDiscPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("5f1f5aec-a783-495b-a560-75dbb8dbd7f6");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.iso"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GcDisc",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["GC"],
            LongDescription = "The DVD image format for GameCube."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new GcDiscState();
        }
    }
}
