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

namespace plugin_yuusha_shisu.Images
{
    public class BtxPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("df2a52a8-9cbe-4959-a593-ad62ae687c17");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.btx" };
        public PluginMetadata Metadata { get; }

        public BtxPlugin()
        {
            Metadata = new PluginMetadata("BTX", "IcySon55;onepiecefreak", "The image resource for Death of a Hero");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "btx\0";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new BtxState();
        }
    }
}
