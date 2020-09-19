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

namespace plugin_level5._3DS.Images
{
    public class ImgcPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("898c9151-71bd-4638-8f90-6d34f0a8600c");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.xi" };
        public PluginMetadata Metadata { get; }

        public ImgcPlugin()
        {
            Metadata = new PluginMetadata("XI", "onepiecefreak", "Main image for 3DS Level-5 games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "IMGC";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new ImgcState();
        }
    }
}
