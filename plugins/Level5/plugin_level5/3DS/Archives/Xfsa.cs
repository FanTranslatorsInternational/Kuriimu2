using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;
using plugin_level5.Compression;

namespace plugin_level5._3DS.Archives
{
    // TODO: Test plugin
    // Game: PWvPL, Inazuma Eleven 2 GO Chrono Stones
    class Xfsa
    {
        private IXfsa _xfsaParser;

        private readonly int _headerSize = Tools.MeasureType(typeof(XfsaHeader));
        private readonly int _directoryEntrySizev1 = Tools.MeasureType(typeof(Xfsa1DirectoryEntry));
        private readonly int _directoryEntrySizev2 = Tools.MeasureType(typeof(Xfsa2DirectoryEntry));

        public IList<ArchiveFileInfo> Load(Stream input)
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
            if (directoryEntrySize == _directoryEntrySizev1)
                _xfsaParser = new XFSA1();
            else if (directoryEntrySize == _directoryEntrySizev2)
                _xfsaParser = new XFSA2();
            else
                throw new InvalidOperationException("Unknown XFSA version.");

            return _xfsaParser.Load(input);
        }

        public void Save(Stream output, IList<ArchiveFileInfo> files, IProgressContext progress)
        {
            if (_xfsaParser == null)
                throw new InvalidOperationException("No XFSA is loaded.");

            _xfsaParser.Save(output, files, progress);
        }
    }
}
