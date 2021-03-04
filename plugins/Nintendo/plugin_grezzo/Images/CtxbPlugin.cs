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

namespace plugin_grezzo.Images
{
    public class CtxbPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("a45653ae-15b9-46d8-8bfe-e5a44159d2b8");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.ctxb" };
        public PluginMetadata Metadata { get; }

        public CtxbPlugin()
        {
            Metadata = new PluginMetadata("CTXB", "onepiecefreak", "The main image resource in 3DS Zelda ports.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "ctxb";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new CtxbState();
        }
    }
}
