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

namespace plugin_nintendo.Images
{
    public class Tex0Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("df9eee90-d390-480a-a6c8-62ac3e241c0d");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new string[0];
        public PluginMetadata Metadata { get; }

        public Tex0Plugin()
        {
            Metadata = new PluginMetadata("TEX0", "onepiecefreak", "The texture resource used in BRRES.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "TEX0";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new Tex0State();
        }
    }
}
