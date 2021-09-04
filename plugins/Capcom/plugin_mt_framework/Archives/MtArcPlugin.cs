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

namespace plugin_mt_framework.Archives
{
    public class MtArcPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("5a2dfcb6-60d6-4783-acd1-bc7fb4a65f38");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.arc" };
        public PluginMetadata Metadata { get; }

        public MtArcPlugin()
        {
            Metadata = new PluginMetadata("MT ARC", "onepiecefreak", "The main archive resource in Capcom games using the MT Framework.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);
            return magic == "ARC\0" || magic == "\0CRA";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new MtArcState();
        }
    }
}
