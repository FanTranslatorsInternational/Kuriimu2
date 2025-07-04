using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_shade.Archives
{
    public class BlnPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("2f02b4dc-6f95-4c6d-b5e8-b70266f8ec2e");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bln"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BLN",
            Publisher = "Level5",
            Developer = "Shade",
            Platform = ["Wii"],
            LongDescription = "Archive in Inazuma Eleven GO Strikers 2013."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new BlnState();
        }
    }
}
