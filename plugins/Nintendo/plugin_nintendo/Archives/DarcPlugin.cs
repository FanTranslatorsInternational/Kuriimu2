using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    public class DarcPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f49fda83-44d8-42be-bdba-5c6a787edc11");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.arc" };
        public PluginMetadata Metadata { get; }

        public DarcPlugin()
        {
            Metadata = new PluginMetadata("DARC", "onepiecefreak", "Archive found in Nintendo games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "darc";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new DarcState();
        }
    }
}
