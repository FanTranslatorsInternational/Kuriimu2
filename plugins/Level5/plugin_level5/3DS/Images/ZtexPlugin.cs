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

namespace plugin_level5._3DS.Images
{
    public class ZtexPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e131dd95-a61b-4eee-a4fa-48d222ac03d5");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.ztex" };
        public PluginMetadata Metadata { get; }

        public ZtexPlugin()
        {
            Metadata = new PluginMetadata("ZTEX", "onepiecefreak", "The main image resource in Fantasy Life.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br=new BinaryReaderX(fileStream);

            return br.ReadString(4) == "ztex";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new ZtexState();
        }
    }
}
