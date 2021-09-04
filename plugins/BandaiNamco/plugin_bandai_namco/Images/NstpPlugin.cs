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
    public class NstpPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("40f66321-eb99-401e-b510-a2a402741f00");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { ".txp" };
        public PluginMetadata Metadata { get; }

        public NstpPlugin()
        {
            Metadata = new PluginMetadata("NSTP", "onepiecefreak", "Main image resource for Bandai Namco games on Nintendo Switch.");
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

            return magic == "NSTP" || magic2 == "NSTP" || magic3 == "NSTP";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new NstpState();
        }
    }
}
