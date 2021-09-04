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

namespace plugin_sting_entertainment.Images
{
    public class TexPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("dcd694e0-31b5-481c-8cb8-4bec0fc05233");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.tex" };
        public PluginMetadata Metadata { get; }

        public TexPlugin()
        {
            Metadata = new PluginMetadata("TEX", "onepiecefreak", "The main texture in Dungeon Travelers 2.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(8) == "Texture ";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new TexState();
        }
    }
}
