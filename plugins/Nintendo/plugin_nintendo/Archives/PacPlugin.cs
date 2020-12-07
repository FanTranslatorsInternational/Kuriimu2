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

namespace plugin_nintendo.Archives
{
    public class PacPlugin:IFilePlugin,IIdentifyFiles
    {
        public Guid PluginId { get; }
        public PluginType PluginType { get; }
        public string[] FileExtensions { get; }
        public PluginMetadata Metadata { get; }

        public PacPlugin()
        {
            Metadata=new PluginMetadata("PAC","onepiecefreak","The main resource in Mario Party 10.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(3) == "PAC";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new PacState();
        }
    }
}
