using System;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.IO;
using plugin_level5.Archives;

namespace plugin_level5.Fonts
{
    public class XfPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("b1b397c4-9a02-4828-b568-39cad733fa3a");
        public PluginType PluginType => PluginType.Font;
        public string[] FileExtensions => new[] { "*.xf" };
        public PluginMetadata Metadata { get; }

        public XfPlugin()
        {
            Metadata = new PluginMetadata("XF", "onepiecefreak", "Font for 3DS Level-5 games");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            if (br.ReadString(4) != "XPCK")
                return false;

            br.BaseStream.Position = 0;

            var xpck = new Xpck();
            var files = xpck.Load(br.BaseStream);

            return files.Any(x => x.FilePath.GetName() == "FNT.bin");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new XfState(pluginManager);
        }
    }
}
