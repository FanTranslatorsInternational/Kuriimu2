using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_beeworks.Archives
{
    public class TD1Plugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("e8b9f059-7321-4aff-bbb1-a55e06d0bd9f");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "Touch Detective 1",
            Publisher = "BeeWorks",
            Developer = "BeeWorks",
            Platform = ["NDS"],
            LongDescription = "The main archive for Touch Detective 1."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new TD1State();
        }
    }
}
