using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;
using plugin_nintendo.Images;
using plugin_nintendo.NW4C;

namespace plugin_nintendo.BCLIM
{
    public class BxlimPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("cf5ae49f-0ce9-4241-900c-668b5c62ce33");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.bclim", "*.bflim" };
        public PluginMetadata Metadata { get; }

        public BxlimPlugin()
        {
            Metadata = new PluginMetadata("BXLIM", "IcySon55, onepiecefreak", "The BCLIM and BFLIM image containers used in Nintendo 3DS games or newer.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream, ByteOrder.BigEndian);

            // Read byte order
            fileStream.Position = fileStream.Length - 0x24;
            var byteOrder = br.ReadType<ByteOrder>();

            // Read header
            br.ByteOrder = byteOrder;
            fileStream.Position = fileStream.Length - 0x28;
            var header = br.ReadType<NW4CHeader>();

            return (header.magic == "CLIM" || header.magic == "FLIM") && header.fileSize == fileStream.Length;
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new BxlimState();
        }
    }
}
