using System;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_level5.General
{
    public class ImgxPlugin : IFilePlugin, IIdentifyFiles
    {
        private static readonly string[] Magics =
        {
            "IMGC",
            "IMGV",
            "IMGA",
            "IMGN"
        };

        public Guid PluginId => Guid.Parse("79159dba-3689-448f-8343-167d58a54b2c");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.xi" };
        public PluginMetadata Metadata { get; }

        public ImgxPlugin()
        {
            Metadata = new PluginMetadata("IMGx", "onepiecefreak", "Main image resource for Level-5 games on multiple platforms.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            var magic = br.ReadString(4);
            return Magics.Contains(magic);
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new ImgxState(fileManager);
        }
    }
}
