using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.Game;

namespace plugin_mt_framework_preview.Previews
{
    public class GenericGmdPlugin : IGamePlugin
    {
        private static IGamePluginState? _state;

        public Guid PluginId => Guid.Parse("ef1074a3-78b9-4358-adeb-6b58c49173ea");
        public PluginMetadata Metadata => new()
        {
            Author = ["onepiecefreak"],
            Name = "GMD",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["3DS", "Switch", "PC"],
            LongDescription = "Preview plugin for Ace Attorney 5."
        };

        public IGamePluginState CreatePluginState(UPath filePath, IReadOnlyList<TextEntry> entries, IPluginFileManager pluginFileManager)
        {
            return _state ??= new GenericGmdState();
        }
    }
}
