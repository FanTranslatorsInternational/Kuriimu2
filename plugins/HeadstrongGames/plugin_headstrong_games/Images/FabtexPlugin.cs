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

namespace plugin_headstrong_games.Images
{
    public class FabtexPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("7508c096-591b-44b2-b0f0-c8495b862ec0");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.fabtex" };
        public PluginMetadata Metadata { get; }

        public FabtexPlugin()
        {
            Metadata = new PluginMetadata("FABTEX", "onepiecefreak", "The main image resource in Pokemon Art Academy.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);

            fileStream.Position += 0x4;
            var magic2 = br.ReadString(4);

            return magic == "FBRC" && magic2 == "TXTR";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new FabtexState(fileManager);
        }
    }
}
