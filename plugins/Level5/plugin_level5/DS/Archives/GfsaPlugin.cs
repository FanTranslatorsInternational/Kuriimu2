using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.Entry;

namespace plugin_level5.DS.Archives
{
    public class GfsaPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f38e0ef3-f6ad-42d8-bb52-a1d3323d5372");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.fa"};
        public PluginMetadata Metadata { get; }

        public GfsaPlugin()
        {
            Metadata = new PluginMetadata("GFSA", "onepiecefreak", "Main resource archive in Professor Layton 4.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "GFSA";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new GfsaState();
        }
    }
}
