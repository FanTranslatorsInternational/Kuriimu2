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

namespace plugin_level5._3DS.Archives
{
    public class XpckPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("de276e88-fb2b-48a6-a55f-d6c14ec60d4f");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.xr", "*.xc", "*.xa", "*.xk" };
        public PluginMetadata Metadata { get; }

        public XpckPlugin()
        {
            Metadata = new PluginMetadata("XPCK", "onepiecefreak", "Main archive for 3DS Level-5 games");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "XPCK";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new XpckState();
        }
    }
}
