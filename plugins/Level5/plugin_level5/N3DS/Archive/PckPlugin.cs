using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.N3DS.Archive
{
    public class PckPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("65178a15-caf5-4f3f-8ece-beb3e4308d0c");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.pck"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "PCK",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["3DS"],
            LongDescription = "General game archive for 3DS Level-5 games"
        };

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new PckState();
        }
    }
}
