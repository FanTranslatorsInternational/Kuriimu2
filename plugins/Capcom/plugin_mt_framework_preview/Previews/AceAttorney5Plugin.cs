using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.Game;

namespace plugin_mt_framework_preview.Previews
{
    public class AceAttorney5Plugin : IGamePlugin
    {
        private static IGamePluginState? _state;

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

        public IGamePluginState CreatePluginState(UPath filePath, IReadOnlyList<TextEntry> entries, IPluginFileManager pluginFileManager)
        {
            return _state ??= new AceAttorney5State(pluginFileManager);
        }
    }
}
