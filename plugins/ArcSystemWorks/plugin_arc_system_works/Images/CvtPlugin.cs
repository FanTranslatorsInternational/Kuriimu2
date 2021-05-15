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
    public class CvtPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("73968c1e-a7f0-402d-9178-5eabeab8b2a8");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.cvt"};
        public PluginMetadata Metadata { get; }

        public CvtPlugin()
        {
            Metadata=new PluginMetadata("CVT","onepiecefreak","The main image resource in Chase: Cold Case Investigations.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(2) == "n\0";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new CvtState();
        }
    }
}
