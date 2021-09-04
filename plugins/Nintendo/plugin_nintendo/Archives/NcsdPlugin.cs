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
    public class NcsdPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("1f11bf6d-da13-43ea-9398-237327414a5d");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.3ds", "*.cci" };
        public PluginMetadata Metadata { get; }

        public NcsdPlugin()
        {
            Metadata = new PluginMetadata("NCSD", "onepiecefreak", "3DS Content Container.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            br.BaseStream.Position = 0x100;
            return br.ReadString(4) == "NCSD";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new NcsdState();
        }
    }
}
