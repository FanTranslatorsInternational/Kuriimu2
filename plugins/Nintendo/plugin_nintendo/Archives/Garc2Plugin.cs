using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    public class Garc2Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("379f0519-a3c9-4248-9264-0e53d8b6b023");
        public string[] FileExtensions => new[] { "*.garc" };
        public PluginMetadata Metadata { get; }

        public Garc2Plugin()
        {
            Metadata = new PluginMetadata("GARC v2", "onepiecefreak", "One kind of archive in Pokemon games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            var magic = br.ReadString(4);
            br.BaseStream.Position = 0xB;
            return magic == "CRAG" && br.ReadByte() == 2;
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new Garc2State();
        }
    }
}
