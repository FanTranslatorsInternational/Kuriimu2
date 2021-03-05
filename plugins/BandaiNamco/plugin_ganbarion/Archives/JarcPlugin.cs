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

namespace plugin_ganbarion.Archives
{
    public class JarcPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("26dad045-388d-42f3-a625-ec44dbf2060d");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.jarc"};
        public PluginMetadata Metadata { get; }

        public JarcPlugin()
        {
            Metadata=new PluginMetadata("JARC","onepiecefreak","The main archive resource in Ganbarion games on 3DS.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "jARC";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new JarcState();
        }
    }
}
