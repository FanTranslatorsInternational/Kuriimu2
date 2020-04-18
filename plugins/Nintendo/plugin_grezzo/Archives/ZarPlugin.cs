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

namespace plugin_grezzo.Archives
{
    public class ZarPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("184e9010-0c35-4ab9-a556-262cbbd2d452");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.zar" };
        public PluginMetadata Metadata { get; }

        public ZarPlugin()
        {
            Metadata = new PluginMetadata("ZAR", "onepiecefreak", "Main archive type in Zelda - Ocarina of Time.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(3) == "ZAR";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new ZarState();
        }
    }
}
