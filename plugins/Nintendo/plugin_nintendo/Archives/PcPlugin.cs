using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    public class PcPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("50d54c18-cb15-49fc-b002-1210126f502f");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.pc" };
        public PluginMetadata Metadata { get; }

        public PcPlugin()
        {
            Metadata = new PluginMetadata("PC", "onepiecefreak", "Archive found in GARCs.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(2) == "PC";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new PcState();
        }
    }
}
