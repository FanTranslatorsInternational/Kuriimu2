using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_bandai_namco.Archives
{
    public class SegPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("d80be35b-1c9f-4afd-b5a7-9c7e4fade16c");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.BIN"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "SEG",
            Publisher = "Bandai Namco",
            Developer = "BBStudio",
            Platform = ["PS2"],
            LongDescription = "The SEG format in Super Robot Taisen Z."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new SegState();
        }
    }
}
