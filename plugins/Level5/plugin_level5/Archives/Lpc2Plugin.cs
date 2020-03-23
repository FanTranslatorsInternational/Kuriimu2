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

namespace plugin_level5.Archives
{
    public class Lpc2Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("d139ebf0-cba1-4338-b688-d7ed49cad392");
        public string[] FileExtensions => new[] { "*.cani" };
        public PluginMetadata Metadata { get; }

        public Lpc2Plugin()
        {
            Metadata = new PluginMetadata("LPC2", "onepiecefreak", "Archive in Level-5 DS games");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "LPC2";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new Lpc2State();
        }
    }
}
