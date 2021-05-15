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

namespace plugin_nintendo.Archives
{
    public class SarcPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("1be80d18-e44e-43d6-884a-65d0b42bfa20");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.szs", "*.arc", "*.sblarc" };
        public PluginMetadata Metadata { get; }

        public SarcPlugin()
        {
            Metadata = new PluginMetadata("SARC", "onepiecefreak", "A main archive resource in Nintendo games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);
            fileStream.Position = 0x11;
            var magic1 = br.ReadString(4);

            return magic == "SARC" || magic1 == "SARC";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new SarcState();
        }
    }
}
