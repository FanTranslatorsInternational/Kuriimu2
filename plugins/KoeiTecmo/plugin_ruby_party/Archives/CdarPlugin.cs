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

namespace plugin_ruby_party.Archives
{
    public class CdarPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("01d70a40-c200-4741-9db8-2f22f930b975");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.bin"};
        public PluginMetadata Metadata { get; }

        public CdarPlugin()
        {
            Metadata = new PluginMetadata("CDAR", "onepiecefreak", "The main archive for Attack on Titan: Escape from Certain Death on 3DS.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "CDAR";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new CdarState();
        }
    }
}
