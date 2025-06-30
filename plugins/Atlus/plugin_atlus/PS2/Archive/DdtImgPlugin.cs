using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_atlus.PS2.Archive
{
    public class DdtImgPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("ddf8e73e-1037-445f-b3f9-cfd2ce9cbde2");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.ddt", "*.img"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["IcySon55"],
            Name = "DDTIMG",
            Publisher = "Atlus",
            Developer = "Atlus",
            Platform = ["PS2"],
            LongDescription = "Main archive of PS2 Atlus games."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new DdtImgState();
        }
    }
}
