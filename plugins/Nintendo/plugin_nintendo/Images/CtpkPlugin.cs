using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_nintendo.CTPK
{
    public class CtpkPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("5033920c-b6d9-4e44-8f3d-de8380cfce27");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.ctpk" };
        public PluginMetadata Metadata { get; }

        public CtpkPlugin()
        {
            Metadata = new PluginMetadata("CTPK", "onepiecefreak", "", "This is the CTPK image adapter for Kuriimu.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using (var br = new BinaryReaderX(fileStream))
            {
                if (br.BaseStream.Length < 4) return false;
                return br.ReadString(4) == "CTPK";
            }
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new CtpkState();
        }
    }
}
