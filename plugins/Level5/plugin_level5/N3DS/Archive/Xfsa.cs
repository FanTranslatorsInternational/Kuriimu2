using System.Buffers.Binary;
using Konnect.Plugin.File.Archive;
using plugin_level5.Common.Compression;

namespace plugin_level5.N3DS.Archive
{
    // TODO: Test plugin
    // Game: PWvPL, Inazuma Eleven 2 GO Chrono Stones
    public class Xfsa
    {
        private IXfsa _xfsaParser;

        private const int HeaderSize_ = 36;
        private const int DirectoryEntrySizev1_ = 16;
        private const int DirectoryEntrySizev2_ = 24;

        public List<ArchiveFile> Load(Stream input)
        {
            // Determine XFSA version and parser
            var buffer = new byte[4];

            input.Position += 4;
            input.Read(buffer, 0, 4);
            var directoryEntriesOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer);

            input.Position += 0x10;
            input.Read(buffer, 0, 2);
            var directoryEntriesCount = BinaryPrimitives.ReadInt16LittleEndian(buffer);

            input.Position += directoryEntriesOffset - 0x1A;
            var directoryDecompressedSize = Level5Compressor.PeekDecompressedSize(input);

            input.Position -= directoryEntriesOffset;

            var directoryEntrySize = directoryDecompressedSize / directoryEntriesCount;
            if (directoryEntrySize == DirectoryEntrySizev1_)
                _xfsaParser = new XFSA1();
            else if (directoryEntrySize == DirectoryEntrySizev2_)
                _xfsaParser = new XFSA2();
            else
                throw new InvalidOperationException("Unknown XFSA version.");

            return _xfsaParser.Load(input);
        }

        public void Save(Stream output, List<ArchiveFile> files)
        {
            if (_xfsaParser == null)
                throw new InvalidOperationException("No XFSA is loaded.");

            _xfsaParser.Save(output, files);
        }
    }
}
