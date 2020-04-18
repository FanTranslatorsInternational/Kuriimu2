using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    public class XbbPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("4e9136f3-924b-40fa-ad17-446e8f2824aa");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.xbb" };
        public PluginMetadata Metadata { get; }

        public XbbPlugin()
        {
            Metadata = new PluginMetadata("XBB", "onepiecefreak", "One kind of archives found in Rocket Slime 3.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(3) == "XBB";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new XbbState();
        }
    }
}
