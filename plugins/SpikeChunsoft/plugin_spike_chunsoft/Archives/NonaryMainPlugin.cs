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

namespace plugin_spike_chunsoft.Archives
{
    public class NonaryMainPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f7ca4d58-f7de-0999-87bd-77c8074521a4");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public NonaryMainPlugin()
        {
            Metadata = new PluginMetadata("Nonary Games", "Neobeo; onepiecefreak", "The main resource for The Nonary Games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadUInt32() == 0xd7d6a6b8;
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new NonaryMainState();
        }
    }
}
