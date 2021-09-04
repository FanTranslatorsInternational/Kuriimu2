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

namespace plugin_koei_tecmo.Archives
{
    public class CraePlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("a7d2ed59-6a9a-49a9-880f-ba206d6cf029");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.gz" };
        public PluginMetadata Metadata { get; }

        public CraePlugin()
        {
            Metadata = new PluginMetadata("CRAE", "onepiecefreak", "Main archive resource in Blue Reflection.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "CRAE";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new CraeState();
        }
    }
}
