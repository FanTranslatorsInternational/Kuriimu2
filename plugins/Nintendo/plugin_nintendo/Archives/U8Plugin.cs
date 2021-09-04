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

namespace plugin_nintendo.Archives
{
    public class U8Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("410009a3-49ef-4356-b9be-a7685c4f786c");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public U8Plugin()
        {
            Metadata = new PluginMetadata("U8", "onepiecefreak", "The main archive format for Wii games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream, ByteOrder.BigEndian);

            return br.ReadUInt32() == 0x55aa382d;
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new U8State();
        }
    }
}
