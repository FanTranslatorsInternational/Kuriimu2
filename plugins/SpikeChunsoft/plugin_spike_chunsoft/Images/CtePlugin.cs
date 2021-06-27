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

namespace plugin_spike_chunsoft.Images
{
    public class CtePlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("70d8a0ef-0aad-4ef6-b219-aecee241f01c");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.img"};
        public PluginMetadata Metadata { get; }

        public CtePlugin()
        {
            Metadata=new PluginMetadata("CTE","onepiecefreak","One image resource in Pokemon Super Mystery Dungeon.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "\0cte";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new CteState();
        }
    }
}
