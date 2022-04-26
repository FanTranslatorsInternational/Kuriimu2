using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;
using System;
using System.Threading.Tasks;

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
            Metadata = new PluginMetadata("SDT", "Caleb Mabry", "Images for Ghost Trick iOS.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            System.IO.Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using BinaryReaderX br = new BinaryReaderX(fileStream);
            string magic = br.ReadString(3);

            return magic == "sdt";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new SdtState();
        }
    }
}
