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

namespace plugin_gust.Images
{
    public class G1tPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("5e8f5e9d-53da-4777-b15f-41f17355fb44");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.g1t" };
        public PluginMetadata Metadata { get; }

        public G1tPlugin()
        {
            Metadata = new PluginMetadata("G1T", "onepiecefreak", "The main image resource in Gust/KoeiTecmo games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);

            var magic1 = br.ReadString(4);
            var magic2 = br.ReadString(4);

            return (magic1 == "GT1G" || magic1 == "G1TG") && (magic2 == "0600" || magic2 == "0500" || magic2 == "1600");
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new G1tState();
        }
    }
}
