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

namespace plugin_atlus.Images
{
    public class TmxPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("c533c2a1-4fdb-4e2a-bbb5-c07d6bf5a22d");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.tmx" };
        public PluginMetadata Metadata { get; }

        public TmxPlugin()
        {
            Metadata = new PluginMetadata("TMX", "onepiecefreak", "An image resource from Atlus games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);

            fileStream.Position = 8;
            return br.ReadString(4) == "TMX0";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new TmxState();
        }
    }
}
