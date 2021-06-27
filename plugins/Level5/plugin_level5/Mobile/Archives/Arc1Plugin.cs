using System;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_level5.Mobile.Archives
{
    public class Arc1Plugin : IFilePlugin, IIdentifyFiles
    {
        private static readonly byte[] MagidId = { 0xB4, 0x11, 0xC2, 0x02 };

        public Guid PluginId => Guid.Parse("e499eb38-f6b0-4bc8-a846-0ea73cf2907a");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.obb" };
        public PluginMetadata Metadata { get; }

        public Arc1Plugin()
        {
            Metadata = new PluginMetadata("ARC1", "onepiecefreak", "Main data of Professor Layton 1 on Android.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadBytes(4).SequenceEqual(MagidId);
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new Arc1State();
        }
    }
}
