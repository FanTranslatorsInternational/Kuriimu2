using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_koei_tecmo.Archives
{
    public class GzPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("b921e43d-ef03-48ea-bc44-c171ffdda2fb");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.gz"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GZ",
            Publisher = "Koei Tecmo",
            Developer = "Koei Tecmo",
            Platform = ["Switch"],
            LongDescription = "An archive resource found in Persona 5 Strikers."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new GzState();
        }
    }
}
