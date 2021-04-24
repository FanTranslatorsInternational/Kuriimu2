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

namespace plugin_level5.Mobile.Images
{
    public class ImgaPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("c99875f7-4751-443e-93d5-5782f7e05ce6");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.xi" };
        public PluginMetadata Metadata { get; }

        public ImgaPlugin()
        {
            Metadata = new PluginMetadata("IMGA", "onepiecefreak", "Main image resource in Android games by Level5.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "IMGA";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new ImgaState(pluginManager);
        }
    }
}
