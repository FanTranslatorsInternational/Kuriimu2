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

namespace plugin_dotemu.Images
{
    public class XnbPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("08239e71-2ef6-4e88-b0e9-fbc52116ced2");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.xnb"};
        public PluginMetadata Metadata { get; }

        public XnbPlugin()
        {
            Metadata = new PluginMetadata("XNB", "onepiecefreak", "Main image resource for Microsoft.XNA.Framework");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(3) == "XNB";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new XnbState();
        }
    }
}