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
    public class SbPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("daad1871-8a85-4f92-adbd-054ac5a91dc7");
        public string[] FileExtensions => new[] { "*.sb" };
        public PluginMetadata Metadata { get; }

        public SbPlugin()
        {
            Metadata = new PluginMetadata("SB", "onepiecefreak", "Archive found in GARCs.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(2) == "SB";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new SbState();
        }
    }
}
