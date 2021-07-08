using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;
using System;
using System.Threading.Tasks;

namespace plugin_sonic_generations.Text
{
    public class SharpMsgPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("75414333-8C46-4014-A3DE-8227384D6527");

        public PluginType PluginType => PluginType.Text;

        public string[] FileExtensions => new[] { "*.MG" };

        public PluginMetadata Metadata { get; }

        public SharpMsgPlugin()
        {
            Metadata = new PluginMetadata("#MSG", "LITTOMA", "#MSG file found Sonic Generations");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fs = await fileSystem.OpenFileAsync(filePath);

            using var reader = new BinaryReaderX(fs);
            var header = reader.ReadType<SharpMsgHeader>();

            return header.magic == "#MSG";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new SharpMsgState();
        }
    }
}
