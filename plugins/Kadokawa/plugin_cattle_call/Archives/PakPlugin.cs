using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_cattle_call.Archives
{
    public class PakPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("2a4e1bf2-1718-44bd-8a72-8c33a9026fb8");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.pak"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "PAK",
            Publisher = "Kadokawa",
            Developer = "Cattle Call",
            Platform = ["NDS"],
            LongDescription = "The main resource in Metal Max 3."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PakState();
        }
    }
}
