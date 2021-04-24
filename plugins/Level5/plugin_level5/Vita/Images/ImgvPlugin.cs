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

namespace plugin_level5.Vita.Images
{
    public class ImgvPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("d3074789-b244-4818-9d7f-e30b417f2bc4");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.xi" };
        public PluginMetadata Metadata { get; }

        public ImgvPlugin()
        {
            Metadata = new PluginMetadata("XI", "onepiecefreak", "Main image for 3DS Level-5 games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "IMGV";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new ImgvState();
        }
    }
}
