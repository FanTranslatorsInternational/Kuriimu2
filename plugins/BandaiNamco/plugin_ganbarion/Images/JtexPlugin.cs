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

namespace plugin_ganbarion.Images
{
    public class JtexPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("4fa038e1-bcb8-470b-998c-7f6a4ffa20fa");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.jtex" };
        public PluginMetadata Metadata { get; }

        public JtexPlugin()
        {
            Metadata = new PluginMetadata("JTEX", "onepiecefreak", "The main image format in ganbarion games on the 3DS.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "jIMG";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new JtexState();
        }
    }
}
