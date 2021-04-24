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

namespace plugin_vblank_entertainment.Archives
{
    public class BfpPlugin : IFilePlugin,IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("2222afb1-c37b-44fc-86df-919fc4093ee4");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bfp" };
        public PluginMetadata Metadata { get; }

        public BfpPlugin()
        {
            Metadata = new PluginMetadata("BFP", "onepiecefreak", "Main archive from Retro City Rampage DX on 3DS.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "RTFP";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new BfpState();
        }
    }
}
