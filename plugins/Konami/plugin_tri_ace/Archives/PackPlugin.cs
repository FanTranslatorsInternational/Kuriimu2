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

namespace plugin_tri_ace.Archives
{
    public class PackPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("8c81d937-e1a8-42e6-910a-d9911a6a93af");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin", "*.pack" };
        public PluginMetadata Metadata { get; }

        public PackPlugin()
        {
            Metadata = new PluginMetadata("P@CK", "onepiecefreak", "The P@CK archive for Beyond The Labyrinth.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "P@CK";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new PackState();
        }
    }
}
