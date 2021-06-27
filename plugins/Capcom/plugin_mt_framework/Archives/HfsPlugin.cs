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

namespace plugin_mt_framework.Archives
{
    public class HfsPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("5a2dfcb6-60d6-4783-acd2-bc7fb4a65f38");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.arc"};
        public PluginMetadata Metadata { get; }

        public HfsPlugin()
        {
            Metadata=new PluginMetadata("HFS","onepiecefreak","The archive resource found on PS3 games by Capcom.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            
            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "\0SFH";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new HfsState();
        }
    }
}
