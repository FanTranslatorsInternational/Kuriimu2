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

namespace plugin_bandai_namco.Images
{
    public class MtexPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("da18bdbb-a094-4d6f-93ad-c33c7da92881");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.totexk" };
        public PluginMetadata Metadata { get; }

        public MtexPlugin()
        {
            Metadata=new PluginMetadata("TOTEXK","Megaflan","The image format found in Tales of Abyss 3DS");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.PeekString() == "XETM";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new MtexState();
        }
    }
}
