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

namespace plugin_kadokawa.Images
{
    public class CtxPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("b792e3e9-b8ee-431d-98ff-1c0a81155dc6");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.ctx"};
        public PluginMetadata Metadata { get; }

        public CtxPlugin()
        {
            Metadata = new PluginMetadata("CTX", "onepiecefreak", "The main image resource in 3DS Kadokawa games, e.g Highschool DxD");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(8) == "CTX 10 \0";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new CtxState();
        }
    }
}
