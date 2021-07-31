using System;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    public class SarcPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("1be80d18-e44e-43d6-884a-65d0b42bfa20");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.szs", "*.arc", "*.sblarc", "*.zlib" };
        public PluginMetadata Metadata { get; }

        public SarcPlugin()
        {
            Metadata = new PluginMetadata("SARC", "onepiecefreak", "A main archive resource in Nintendo games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            if (br.PeekString() == "SARC")
                return true;

            fileStream.Position = 0x11;
            if (br.PeekString() == "SARC")
                return true;

            using var zlibStream = new InflaterInputStream(new SubStream(fileStream, 4, fileStream.Length - 4)) {IsStreamOwner = false};
            using var zlibBr = new BinaryReaderX(zlibStream);

            return zlibBr.ReadString(4) == "SARC";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new SarcState();
        }
    }
}
