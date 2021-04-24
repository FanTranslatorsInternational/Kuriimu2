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

namespace plugin_capcom.Archives
{
    public class ObbPlugin:IFilePlugin,IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("aa9d1923-e658-4d41-9efe-266748b8cc6d");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.obb"};
        public PluginMetadata Metadata { get; }

        public ObbPlugin()
        {
            Metadata=new PluginMetadata("OBB","onepiecefreak","The main OBB of Capcom mobile MT Framework games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == ".OBB";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new ObbState();
        }
    }
}
