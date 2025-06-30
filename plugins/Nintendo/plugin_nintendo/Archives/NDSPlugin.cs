using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class NDSPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("b79501ec-fb56-4a0a-a4ae-018cdf6fecf3");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.nds"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "NDS",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["NDS"],
            LongDescription = "NDS Cardridge."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new NDSState();
        }
    }
}
