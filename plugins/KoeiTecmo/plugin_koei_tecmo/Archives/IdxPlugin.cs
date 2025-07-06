using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_koei_tecmo.Archives
{
    public class IdxPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("266a0018-e8b7-4921-ab03-e6c639c630ed");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin", "*.idx"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BIN_IDX",
            Publisher = "Koei Tecmo",
            Developer = "Koei Tecmo",
            Platform = ["3DS"],
            LongDescription = "The main resource package in KoeiTecmo games."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new IdxState();
        }
    }
}
