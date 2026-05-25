using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.Game;
using plugin_level5_preview.Preview.Subtitle;

namespace plugin_level5_preview.Preview
{
    public class TimeTravelersSubtitlePlugin : IGamePlugin
    {
        public Guid PluginId => Guid.Parse("06e459a5-cf80-4410-8070-c8c80a094e11");
        public PluginMetadata Metadata => new()
        {
            Author = ["onepiecefreak"],
            Name = "Time Travelers Subtitle",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["3DS", "Vita", "Psp"],
            LongDescription = "Preview plugin for Time Travelers."
        };

        public IGamePluginState CreatePluginState(UPath filePath, IPluginFileManager pluginFileManager)
        {
            return new TimeTravelersSubtitleState(pluginFileManager);
        }
    }
}
