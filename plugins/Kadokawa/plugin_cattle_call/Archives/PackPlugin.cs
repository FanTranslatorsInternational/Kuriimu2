using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_cattle_call.Archives
{
    public class PackPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("74d25496-ec7b-4a4b-8e68-e2a7dae2b118");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions =>[];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "PACK",
            Publisher = "Kadokawa",
            Developer = "Cattle Call",
            Platform = ["3DS"],
            LongDescription = "Extensionless pack files in Metal Max 4."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PackState();
        }
    }
}
