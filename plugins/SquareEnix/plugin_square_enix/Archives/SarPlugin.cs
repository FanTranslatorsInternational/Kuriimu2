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

namespace plugin_square_enix.Archives
{
    public class SarPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("3c55e976-66aa-4d39-abf9-e48f03d5a624");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.sar" };
        public PluginMetadata Metadata { get; }

        public SarPlugin()
        {
            Metadata = new PluginMetadata("SAR", "onepiecefreak", "The Archive resource in Heroes of Mana.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "sar ";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new SarState();
        }
    }
}
