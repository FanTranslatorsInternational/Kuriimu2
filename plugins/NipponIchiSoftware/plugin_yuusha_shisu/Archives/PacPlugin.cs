using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.IO;
using System;
using System.Threading.Tasks;
using Kontract.Models.Context;

namespace plugin_yuusha_shisu.PAC
{
    public class PacPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("0066a5a4-1303-4673-bc7f-1742879c3562");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.pac" };
        public PluginMetadata Metadata { get; }

        public PacPlugin()
        {
            Metadata = new PluginMetadata("PAC", "StorMyu", "Death of a Hero");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using (var br = new BinaryReaderX(fileStream))
                return br.ReadString(4) == "ARC\0";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new PacState();
        }
    }
}
