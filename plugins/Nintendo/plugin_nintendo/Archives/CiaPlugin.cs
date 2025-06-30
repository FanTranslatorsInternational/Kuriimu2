using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class CiaPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("509a72a2-445f-4a62-8a13-7b82d773c03e");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.cia"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "CIA",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["3DS"],
            LongDescription = "Installable 3DS game container."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new CiaState();
        }
    }
}
