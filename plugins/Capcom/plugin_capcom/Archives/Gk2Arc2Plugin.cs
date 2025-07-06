using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_capcom.Archives
{
    public class Gk2Arc2Plugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("20878c48-697c-46f2-9bbd-5b4b1986dbcc");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GK2_2",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["NDS"],
            LongDescription = "The sub resource archive for Gyakuten Kenji 2."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new Gk2Arc2State();
        }
    }
}
