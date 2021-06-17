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

namespace plugin_criware.Archives
{
    public class CvmPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("6fc77f8b-7811-4820-a1b8-c0708d898652");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.cvm" };
        public PluginMetadata Metadata { get; }

        public CvmPlugin()
        {
            Metadata = new PluginMetadata("CVM", "onepiecefreak", "The main archive resource by Cri Middleware in the PS2 era of games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "CVMH";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new CvmState();
        }
    }
}
