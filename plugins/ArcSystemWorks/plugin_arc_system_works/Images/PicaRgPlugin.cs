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

namespace plugin_arc_system_works.Images
{
    public class PicaRgPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("49682fbd-3c86-4b40-93f3-8bfc1bbbd53b");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.lzb"};
        public PluginMetadata Metadata { get; }

        public PicaRgPlugin()
        {
            Metadata=new PluginMetadata("PicaRg","onepiecefreak","The main image resource in Jake Hunter by Arc System Works.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(6) == "picaRg";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new PicaRgState();
        }
    }
}
