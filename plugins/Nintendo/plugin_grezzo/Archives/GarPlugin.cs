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

namespace plugin_grezzo.Archives
{
    public class GarPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("bf1e60d4-2613-46d0-a338-b94befabc889");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.gar" };
        public PluginMetadata Metadata { get; }

        public GarPlugin()
        {
            Metadata = new PluginMetadata("GAR", "onepiecefreak", "Main archive type in Zelda - Majoras Mask.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(3) == "GAR";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new GarState();
        }
    }
}
