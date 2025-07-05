using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_kadokawa.Archives
{
    public class EncPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("be692456-7296-44ac-91d5-d378dc6c51a3");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.enc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "ENC",
            Publisher = "Kadokawa",
            Developer = "Kadokawa",
            Platform = ["3DS"],
            LongDescription = "An archive for Highschool DxD on 3DS."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new EncState();
        }
    }
}
