using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    public class Garc4Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("fa49a481-8673-4360-beb5-ccd34961df1b");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.garc" };
        public PluginMetadata Metadata { get; }

        public Garc4Plugin()
        {
            Metadata = new PluginMetadata("GARC v4", "onepiecefreak", "One kind of archive in Pokemon games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            var magic = br.ReadString(4);
            br.BaseStream.Position = 0xB;
            return magic == "CRAG" && br.ReadByte() == 4;
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new Garc4State();
        }
    }
}
