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

namespace plugin_level5._3DS.Archives
{
    public class B123Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("948cde6d-e0e8-4518-a38a-9ba5bf6d4e9e");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.fa" };
        public PluginMetadata Metadata { get; }

        public B123Plugin()
        {
            Metadata = new PluginMetadata("B123", "onepiecefreak", "Main game archive for older 3DS Level-5 games");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "B123";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new B123State();
        }
    }
}
