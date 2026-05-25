using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.Game;
using plugin_level5_preview.Preview.Narration;

namespace plugin_level5_preview.Preview
{
    public class TimeTravelersNarrationPlugin : IGamePlugin
    {
        public Guid PluginId => Guid.Parse("a21a4442-ead0-4707-9b3d-caf7806e3a47");
        public PluginMetadata Metadata => new()
        {
            Author = ["onepiecefreak"],
            Name = "Time Travelers Narration",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["3DS", "Vita", "Psp"],
            LongDescription = "Preview plugin for Time Travelers."
        };

        public IGamePluginState CreatePluginState(UPath filePath, IPluginFileManager pluginFileManager)
        {
            return new TimeTravelersNarrationState(pluginFileManager);
        }
    }
}
