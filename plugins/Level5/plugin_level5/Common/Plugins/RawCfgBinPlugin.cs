using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.Common.Plugins
{
    public class RawCfgBinPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("f5a28791-fb56-451b-b513-c042d706f4b3");
        public PluginType PluginType => PluginType.Text;
        public string[] FileExtensions => ["*.cfg.bin"];
        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "Raw CFGBIN",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["PSP", "Vita", "3DS"],
            LongDescription = "Main text resource in Time Travelers."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new RawCfgBinState();
        }
    }
}
