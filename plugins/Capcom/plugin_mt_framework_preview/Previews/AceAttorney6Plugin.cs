using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.Game;

namespace plugin_mt_framework_preview.Previews
{
    public class AceAttorney6Plugin : IGamePlugin
    {
        private static IGamePluginState? _state;

        public Guid PluginId => Guid.Parse("a1fecf11-70aa-49f1-af6f-498d5ff2de41");
        public PluginMetadata Metadata => new()
        {
            Author = ["onepiecefreak"],
            Name = "Ace Attorney 6",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["3DS", "Switch", "PC"],
            LongDescription = "Preview plugin for Ace Attorney 6."
        };

        public IGamePluginState CreatePluginState(UPath filePath, IReadOnlyList<TextEntry> entries, IPluginFileManager pluginFileManager)
        {
            return _state ??= new AceAttorney6State(pluginFileManager);
        }
    }
}
