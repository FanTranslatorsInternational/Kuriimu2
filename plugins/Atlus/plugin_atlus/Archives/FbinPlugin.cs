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

namespace plugin_atlus.Archives
{
    public class FbinPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e61606a4-d5ea-4fc2-9173-084757e170eb");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.bin"};
        public PluginMetadata Metadata { get; }

        public FbinPlugin()
        {
            Metadata = new PluginMetadata("FBIN", "onepiecefreak", "One resource archive in Atlus games on 3DS.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "FBIN";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new FbinState();
        }
    }
}
