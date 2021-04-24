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

namespace plugin_atlus.Images
{
    public class StexPlugin:IFilePlugin,IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("b1e1d2cf-4fdc-4b81-b34f-5b3c03a32d40");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.stex"};
        public PluginMetadata Metadata { get; }

        public StexPlugin()
        {
            Metadata=new PluginMetadata("STEX","onepiecefreak","The main image resource in Atlus games on 3DS.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "STEX";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new StexState();
        }
    }
}
