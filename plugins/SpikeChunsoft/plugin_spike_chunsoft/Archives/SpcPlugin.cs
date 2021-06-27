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

namespace plugin_spike_chunsoft.Archives
{
    public class SpcPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("aa2db4be-250c-4412-8811-05b9060fd418");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.spc" };
        public PluginMetadata Metadata { get; }

        public SpcPlugin()
        {
            Metadata = new PluginMetadata("SPC", "onepiecefreak", "The main archive in Danganronpa 3.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "CPS.";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new SpcState();
        }
    }
}
