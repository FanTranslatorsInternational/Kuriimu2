using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_felistella.Archives
{
    public class PacPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("e7e1f311-fb7e-4be5-bfba-469abe2c927f");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.PAC"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "PAC",
            Publisher = "Felistella",
            Developer = "Felistella",
            Platform = ["Vita"],
            LongDescription = "The package resource in Genkai Tokki Seven Pirates."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PacState();
        }
    }
}
