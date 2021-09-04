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

namespace plugin_sega.Images
{
    public class HtxPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("9abf1cd7-be79-43b1-b99b-63bead36aaf0");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.HTX"};
        public PluginMetadata Metadata { get; }

        public HtxPlugin()
        {
            Metadata=new PluginMetadata("HTEX","onepiecefreak","The main image resource in Sakura Wars V and games from the same developer.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "HTEX";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new HtexState();
        }
    }
}
