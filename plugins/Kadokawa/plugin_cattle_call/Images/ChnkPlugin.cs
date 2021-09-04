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

namespace plugin_cattle_call.Images
{
    public class ChnkPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f41f9666-1a3f-4011-9231-4b27302d54f5");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.tex" };
        public PluginMetadata Metadata { get; }

        public ChnkPlugin()
        {
            Metadata = new PluginMetadata("CHNK", "onepiecefreak", "Image resource in Metal Max 3.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "CHNK";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new ChnkState();
        }
    }
}
