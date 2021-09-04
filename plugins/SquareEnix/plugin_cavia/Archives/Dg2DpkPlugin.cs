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

namespace plugin_cavia.Archives
{
    public class Dg2DpkPlugin:IFilePlugin,IIdentifyFiles
    {
        public Guid PluginId { get; }
        public PluginType PluginType { get; }
        public string[] FileExtensions { get; }
        public PluginMetadata Metadata { get; }

        public Dg2DpkPlugin()
        {
            Metadata=new PluginMetadata("DPK","onepiecefreak","The main archive in Drakengard 2.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(3) == "dpk";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new Dg2DpkState();
        }
    }
}
