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

namespace plugin_spike_chunsoft.Archives
{
    public class ZdpPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("577d64ef-fe0d-4a17-a9e9-86f75041a392");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.zdp"};
        public PluginMetadata Metadata { get; }

        public ZdpPlugin()
        {
            Metadata = new PluginMetadata("ZDP", "onepiecefreak", "The datapack for Spike Chunsoft games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(8) == "datapack";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new ZdpState();
        }
    }
}
