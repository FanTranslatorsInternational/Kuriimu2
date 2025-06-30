using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class ViewPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("a32c6750-7907-4abc-b009-47a5b6fd1251");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.viw"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "VIW",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["NDS"],
            LongDescription = "The lib resource from Tingle Baloon Trip."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new ViwState();
        }
    }
}
