using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_bandai_namco.Archives
{
    public class _3dsLzPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("863a38e7-69e8-4a53-8045-d864661cb65b");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "3DS-LZ",
            Publisher = "Bandai Namco",
            Developer = "Bandai Namco",
            Platform = ["3DS"],
            LongDescription = "The archive used in Dragon Ball Heroes Ultimate Mission"
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new _3dsLzState();
        }
    }
}