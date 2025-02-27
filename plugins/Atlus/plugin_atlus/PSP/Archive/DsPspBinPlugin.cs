using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using plugin_atlus.N3DS.Archive;

namespace plugin_atlus.PSP.Archive
{
    public class DsPspBinPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("97C4C1A0-F375-49CD-AA3E-2621A6827D0B");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; } = new()
        {
            Name = "DsPspBin",
            Author = "Megaflan",
            LongDescription = "The Bin resource container seen in Shin Megami Tensei: Devil Summoner and Persona 2 (PSP)."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginManager)
        {
            return new DsPspBinState();
        }
    }
}
