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
    public class ArcvPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("db8c2deb-f11d-43c8-bb9e-e271408fd896");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.arc" };
        public PluginMetadata Metadata { get; }

        public ArcvPlugin()
        {
            Metadata = new PluginMetadata("ARCV", "onepiecefreak", "Generic archive for 3DS Level-5 games");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "ARCV";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new ArcvState();
        }
    }
}
