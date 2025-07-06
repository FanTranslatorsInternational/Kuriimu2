using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_koei_tecmo.Archives
{
    public class SangoBinPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("aa6a43ce-ff5f-4bc5-b8b4-fe1b84a5d40e");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BIN",
            Publisher = "Koei Tecmo",
            Developer = "Koei Tecmo",
            Platform = ["3DS"],
            LongDescription = "One file archive found in Yo-Kai Watch Sangokushi."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new SangoBinState();
        }
    }
}
