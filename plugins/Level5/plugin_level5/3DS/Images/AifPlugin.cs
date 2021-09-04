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
    public class AifPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("eab51bcd-385b-4b06-b622-2a433cfc4530");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.aif"};
        public PluginMetadata Metadata { get; }

        public AifPlugin()
        {
            Metadata=new PluginMetadata("AIF","onepiecefreak","Main image resource in Danball Senki by Level5.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == " FIA";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new AifState();
        }
    }
}
