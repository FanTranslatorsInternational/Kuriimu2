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

namespace plugin_criware.Archives
{
    public class CpkPlugin:IFilePlugin,IIdentifyFiles
    {
        public Guid PluginId { get; }
        public PluginType PluginType { get; }
        public string[] FileExtensions { get; }
        public PluginMetadata Metadata { get; }

        public CpkPlugin()
        {
            Metadata=new PluginMetadata("CPK","IcySon55","The main archive for the CriWare Middleware.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "CPK ";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new CpkState();
        }
    }
}
