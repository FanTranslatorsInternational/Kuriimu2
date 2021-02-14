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

namespace plugin_level5.DS.Archives
{
    public class GfspPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("0fc27e6a-f61e-426f-93c2-62550646ea89");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.ca", "*.cb" };
        public PluginMetadata Metadata { get; }

        public GfspPlugin()
        {
            Metadata = new PluginMetadata("GFSP", "onepiecefreak", "The main resource archive in Level5 games on DS.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "GFSP";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new GfspState();
        }
    }
}
