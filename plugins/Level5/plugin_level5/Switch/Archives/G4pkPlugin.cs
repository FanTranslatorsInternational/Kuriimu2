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

namespace plugin_level5.Switch.Archives
{
    public class G4pkPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("0964a630-2ca3-4063-8e53-bf7210cbc70e");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.g4pk","*.g4pkm" };
        public PluginMetadata Metadata { get; }

        public G4pkPlugin()
        {
            Metadata = new PluginMetadata("G4PK", "onepiecefreak", "Game archive for Switch Level-5 games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "G4PK";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new G4pkState();
        }
    }
}
