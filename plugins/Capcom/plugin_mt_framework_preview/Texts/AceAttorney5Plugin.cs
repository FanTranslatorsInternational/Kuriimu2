using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.Game;

namespace plugin_mt_framework_preview.Texts
{
    public class AceAttorney5Plugin : IGamePlugin
    {
        public Guid PluginId => Guid.Parse("1280108e-010d-4bf0-a495-e614f340360c");
        public PluginMetadata Metadata => new()
        {
            Author = ["onepiecefreak"],
            Name = "Ace Attorney 5",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["3DS", "Switch", "PC"],
            LongDescription = "Preview plugin for Ace Attorney 5."
        };

        public IGamePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new AceAttorney5State(pluginFileManager);
        }
    }
}
