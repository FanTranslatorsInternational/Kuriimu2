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

namespace plugin_level5.Switch.Images
{
    public class NxtchPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("89222f8f-a345-45ed-9b79-e9e873bda1e9");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.nxtch" };
        public PluginMetadata Metadata { get; }

        public NxtchPlugin()
        {
            Metadata = new PluginMetadata("NXTCH", "onepiecefreak", "The main image resource in some Level5 Switch games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(5) == "NXTCH";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new NxtchState();
        }
    }
}
