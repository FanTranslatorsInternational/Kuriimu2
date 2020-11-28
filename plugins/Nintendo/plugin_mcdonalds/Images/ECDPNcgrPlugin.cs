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

namespace plugin_mcdonalds.Images
{
    public class ECDPNcgrPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId =>Guid.Parse("805c26f1-9d54-ecd5-ac84-2628eec5baa5");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.ncgr"};
        public PluginMetadata Metadata { get; }

        public ECDPNcgrPlugin()
        {
            Metadata = new PluginMetadata("eCDP NCGR", "onpiecefreak", "NCGR's found in eCDP by McDonalds.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "RGCN";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new ECDPNcgrState();
        }
    }
}
