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

namespace plugin_sting_entertainment.Archives
{
    public class PckPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("dcdd3e5f-ba74-4541-a870-5d705ed6471a");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.pck" };
        public PluginMetadata Metadata { get; }

        public PckPlugin()
        {
            Metadata = new PluginMetadata("PCK", "onepiecefreak", "The main package resource in Dungeon Travelers 2.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(8) == "Filename";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new PckState();
        }
    }
}
