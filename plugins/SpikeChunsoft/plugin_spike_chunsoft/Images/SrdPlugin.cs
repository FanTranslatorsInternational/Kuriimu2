using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_spike_chunsoft.Images
{
    public class SrdPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("5fb1ce7a-d657-461b-a282-9659db7337a1");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.srd", "*.srdv" };
        public PluginMetadata Metadata { get; }

        public SrdPlugin()
        {
            Metadata = new PluginMetadata("SRD", "onepiecefreak", "The main image resource in Danganronpa 3.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            filePath = filePath.GetExtensionWithDot() == ".srd" ? filePath : filePath.ChangeExtension(".srd");
            if (!fileSystem.FileExists(filePath))
                return false;

            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "$CFH";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new SrdState();
        }
    }
}
