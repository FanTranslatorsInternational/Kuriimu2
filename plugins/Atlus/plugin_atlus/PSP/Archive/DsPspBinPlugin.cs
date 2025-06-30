using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_atlus.PSP.Archive
{
    public class DsPspBinPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("97C4C1A0-F375-49CD-AA3E-2621A6827D0B");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["Megaflan"],
            Name = "DsPspBin",
            Publisher = "Atlus",
            Developer = "Atlus",
            Platform = ["PSP"],
            LongDescription = "The Bin resource container seen in Shin Megami Tensei: Devil Summoner and Persona 2 (PSP)."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginManager)
        {
            return new DsPspBinState();
        }
    }
}
