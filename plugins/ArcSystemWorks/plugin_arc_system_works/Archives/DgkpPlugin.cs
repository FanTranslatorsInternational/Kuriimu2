using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_arc_system_works.Archives
{
    public class DgkpPlugin:IFilePlugin,IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("03e56bf8-493d-4a92-885a-0bdf104b258e");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.pac"};
        public PluginMetadata Metadata { get; }

        public DgkpPlugin()
        {
            Metadata=new PluginMetadata("DGKP","onepiecefreak","A resource archive of Chase: Cold Case Investigations on 3DS.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "DGKP";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new DgkpState();
        }
    }
}
