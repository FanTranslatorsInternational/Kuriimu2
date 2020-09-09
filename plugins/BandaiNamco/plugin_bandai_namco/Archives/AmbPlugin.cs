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

namespace plugin_bandai_namco.Archives
{
    public class AmbPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f701c40e-d7e8-4413-b3de-91eafbca450a");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.amb", "*.AMB" };
        public PluginMetadata Metadata { get; }

        public AmbPlugin()
        {
            Metadata = new PluginMetadata("AMB", "onepiecefreak", "The resource archive used in Dragon Ball Heroes games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "#AMB";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new AmbState();
        }
    }
}
