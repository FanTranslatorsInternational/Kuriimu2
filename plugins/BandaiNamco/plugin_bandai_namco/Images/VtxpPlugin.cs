using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_bandai_namco.Images
{
    public class VtxpPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("a10d9fe1-3c86-44f4-9585-454afc432393");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { ".txp" };
        public PluginMetadata Metadata { get; }

        public VtxpPlugin()
        {
            Metadata = new PluginMetadata("VTXP", "onepiecefreak", "Main image resource for Bandai Namco games on Sony PS Vita.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);

            // Magics for possible compressions
            fileStream.Position++;
            var magic2 = br.ReadString(4);
            var magic3 = br.ReadString(4);

            return magic == "VTXP" || magic2 == "VTXP" || magic3 == "VTXP";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new VtxpState();
        }
    }
}
