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

namespace plugin_capcom.Images
{
    public class SdtPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("7b8ec4f7-7e9e-4b68-9945-0bcfd294f98a");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.sdt" };
        public PluginMetadata Metadata { get; }

        public SdtPlugin()
        {
            Metadata = new PluginMetadata("SDT", "Caleb Mabry", "Main image resource.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(3);

            return magic == "sdt";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new SdtState();
        }
    }
}
