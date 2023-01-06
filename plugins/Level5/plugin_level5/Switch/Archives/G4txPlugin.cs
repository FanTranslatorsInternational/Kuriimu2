using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.Entry;

namespace plugin_level5.Switch.Archives
{
    public class G4txPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("ae6bc510-096b-4dcd-ba9c-b67985d2bed2");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.g4tx" };
        public PluginMetadata Metadata { get; }

        public G4txPlugin()
        {
            Metadata = new PluginMetadata("G4TX", "onepiecefreak", "The main image resource container in some Level5 Switch games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "G4TX";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new G4txState();
        }
    }
}
