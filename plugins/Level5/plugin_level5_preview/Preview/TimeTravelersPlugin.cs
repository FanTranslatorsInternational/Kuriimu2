using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.Game;

namespace plugin_level5_preview.Preview
{
    public class TimeTravelersPlugin : IGamePlugin
    {
        public Guid PluginId => Guid.Parse("a21a4442-ead0-4707-9b3d-caf7806e3a47");
        public PluginMetadata Metadata => new()
        {
            Author = ["onepiecefreak"],
            Name = "Time Travelers",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["3DS", "Vita", "Psp"],
            LongDescription = "Preview plugin for Time Travelers."
        };

        public IGamePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new TimeTravelersState(pluginFileManager);
        }
    }
}
