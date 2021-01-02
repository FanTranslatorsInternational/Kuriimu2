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
    public class GxtPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("b7453fd6-ca66-4684-b172-8f51db77ea75");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.gxt", "*.bin" };
        public PluginMetadata Metadata { get; }

        public GxtPlugin()
        {
            Metadata=new PluginMetadata("GXT","onepiecefreak, IcySon55","The image format found in [...]");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.PeekString() == "GXT\0";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new GxtState();
        }
    }
}
