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

namespace plugin_headstrong_games.Archives
{
    public class FabPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("c112dde7-b983-4c63-9c06-9e4fbfee04d5");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.fab"};
        public PluginMetadata Metadata { get; }

        public FabPlugin()
        {
            Metadata = new PluginMetadata("FAB", "onepiecefreak", "The main file resource in Pokemon Art Academy.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);

            fileStream.Position += 0x4;
            var magic2 = br.ReadString(4);

            return magic == "FBRC" && magic2 == "BNDL";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new FabState();
        }
    }
}
