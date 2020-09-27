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

namespace plugin_bandai_namco.Archives
{
    public class L7cPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("067f4da2-98b5-43f5-9698-d77c81184642");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.l7c"};
        public PluginMetadata Metadata { get; }

        public L7cPlugin()
        {
            Metadata=new PluginMetadata("L7C","onepiecefreak","The resource archive in Tales Of games on PS Vita.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "L7CA";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new L7cState();
        }
    }
}
