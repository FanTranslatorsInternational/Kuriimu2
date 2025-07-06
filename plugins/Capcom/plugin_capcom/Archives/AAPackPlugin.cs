using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_capcom.Archives
{
    public class AAPackPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("8a86b7a5-7a3b-4d7f-95e6-31c417b0f4a8");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.inc", "*.dat"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "AAPack",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["3DS"],
            LongDescription = "The main archive of Ace Attorney Trilogy and Apollo Justice 3DS."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new AAPackState();
        }
    }
}
