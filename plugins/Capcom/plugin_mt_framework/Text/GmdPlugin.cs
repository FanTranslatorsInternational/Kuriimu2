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

namespace plugin_mt_framework.Text
{
#if DEBUG

    public class GmdPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("1b1cce5b-de7a-4e9e-be5e-203162875c2d");
        public PluginType PluginType => PluginType.Text;
        public string[] FileExtensions => new[] {"*.gmd"};
        public PluginMetadata Metadata { get; }

        public GmdPlugin()
        {
            Metadata = new PluginMetadata("GMD", "onepiecefreak", "The main text resource in Capcom games using the MT Framework.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);

            return magic == "GMD\0" || magic == "\0DMG";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new GmdState();
        }
    }

#endif
}
