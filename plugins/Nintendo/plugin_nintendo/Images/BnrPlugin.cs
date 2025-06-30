using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Images
{
    public class BnrPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("f1fd5589-550d-4916-a358-4866e0e904e1");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.bnr", "*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BNR",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["NDS"],
            LongDescription = "The DS Banner format."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new BnrState();
        }
    }
}
