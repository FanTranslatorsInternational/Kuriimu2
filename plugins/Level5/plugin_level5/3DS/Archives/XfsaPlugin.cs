using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.Entry;

namespace plugin_level5._3DS.Archives
{
    public class XfsaPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f712c7ef-1585-48a2-857c-86d0f40054fb");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.fa" };
        public PluginMetadata Metadata { get; }

        public XfsaPlugin()
        {
            Metadata = new PluginMetadata("XFSA", "onepiecefreak", "Main game archive for 3DS Level-5 games");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "XFSA";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new XfsaState();
        }
    }
}
