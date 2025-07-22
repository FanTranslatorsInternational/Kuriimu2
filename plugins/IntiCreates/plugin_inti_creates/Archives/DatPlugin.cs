using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_inti_creates.Archives
{
    public class DatPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("5882b51c-d553-4f8c-9843-6d022f153d99");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.dat"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "DAT",
            Publisher = "Inti Creates",
            Developer = "Inti Creates",
            Platform = ["3DS"],
            LongDescription = "A data resource found in Azure Strikers Gunvolt."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new DatState();
        }
    }
}
