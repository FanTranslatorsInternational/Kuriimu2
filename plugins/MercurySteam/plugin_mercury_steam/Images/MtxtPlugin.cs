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

namespace plugin_mercury_steam.Images
{
    public class MtxtPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("c21befd9-2854-45fb-889f-6e42d374c1f3");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public MtxtPlugin()
        {
            Metadata = new PluginMetadata("MTXT", "onepiecefreak", "The main image resource in Mercury Steam games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "MTXT";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new MtxtState(fileManager);
        }
    }
}
