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

namespace plugin_nintendo.Images
{
    public class _3dstPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("1fa3a56c-864c-4618-9dfc-22734b91d4c5");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.3dst" };
        public PluginMetadata Metadata { get; }

        public _3dstPlugin()
        {
            Metadata = new PluginMetadata("3DST", "DaniElectra", "The image format used for textures and thumbnails on Nintendo Anime Channel.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(7);
            return magic == "texture";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new _3dstState();
        }
    }
}
