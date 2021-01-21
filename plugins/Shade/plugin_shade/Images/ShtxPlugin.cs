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

namespace plugin_shade.Images
{
    public class ShtxPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("01EB4BEE-8C72-44D2-B6B8-13791DEFA487");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.shtx", "*.btx" };
        public PluginMetadata Metadata { get; }

        public ShtxPlugin()
        {
            Metadata = new PluginMetadata("SHTX", "Obluda", "Images for Shade games");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using (var br = new BinaryReaderX(fileStream))
                return br.ReadString(4) == "SHTX";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new ShtxState();
        }
    }
}