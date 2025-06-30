using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_dotemu.Archives
{
    public class Sor4Plugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("bab218f4-550f-40ee-9219-d83b11265883");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => [];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "SOR4",
            Publisher = "DotEmu",
            Developer = "DotEmu",
            Platform = ["Switch"],
            LongDescription = "The main texture resource archive in Streets Of Rage 4."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new Sor4State();
        }
    }
}
