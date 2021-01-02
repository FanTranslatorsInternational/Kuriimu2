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
    public class NARCPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("2033a334-3c14-413c-af28-7e1f95f93bd0");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.narc" };
        public PluginMetadata Metadata { get; }

        public NARCPlugin()
        {
            Metadata = new PluginMetadata("NARC", "onepiecefreak", "Standard resource archive on NDS.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "NARC";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new NARCState();
        }
    }
}
