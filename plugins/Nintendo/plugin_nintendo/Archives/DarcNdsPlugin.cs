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
    public class DarcNdsPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e33b7ba1-5dd3-4afe-b2f3-754e29fc85b1");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.darc" };
        public PluginMetadata Metadata { get; }

        public DarcNdsPlugin()
        {
            Metadata = new PluginMetadata("DARC NDS", "onepiecefreak", "DARC resource archive on NDS systems.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "DARC";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new DarcNdsState();
        }
    }
}
