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

namespace plugin_konami.Archives
{
    public class PackPlugin:IFilePlugin,IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("a4615fdf-f408-4d22-a3fe-17f082f974e0");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.pack"};
        public PluginMetadata Metadata { get; }

        public PackPlugin()
        {
            Metadata=new PluginMetadata("PACK","onepiecefreak","The resource archive in New Love Plus.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "PACK";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new PackState();
        }
    }
}
