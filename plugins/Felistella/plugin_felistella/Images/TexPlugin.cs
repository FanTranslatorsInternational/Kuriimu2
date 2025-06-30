using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_felistella.Images
{
    public class TexPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("4a29ddcc-bf9b-4fba-a5cd-6291fed13f23");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.tex_vita"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "TEX_VITA",
            Publisher = "Felistella",
            Developer = "Felistella",
            Platform = ["Vita"],
            LongDescription = "The main image resource in Genka Tikko Seven Pirates."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new TexState();
        }
    }
}
