using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_inti_creates.Archives
{
    public class VapPlugin : IFilePlugin
    {
        public Guid PluginId =>Guid.Parse("e38a0292-5e7d-457f-8795-8e0a1c44900f");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.vap","*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "VAP",
            Publisher = "Inti Creates",
            Developer = "Inti Creates",
            Platform = ["3DS"],
            LongDescription = "An archive for Azure Striker Gunvolt on 3DS."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new VapState();
        }
    }
}
