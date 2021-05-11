using System;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Models.Archive;

namespace plugin_inti_creates.Archives
{
    class Dat
    {
        private static int HeaderSize = Tools.MeasureType(typeof(DatHeader));
        private static int SubHeaderSize = Tools.MeasureType(typeof(DatSubHeader));

        public IArchiveFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            var header = br.ReadType<DatHeader>();

            // Decompress file
            var ms = new MemoryStream();
            Compressions.IrLz.Build().Decompress(new SubStream(input, header.dataOffset, header.fileSize - header.dataOffset), ms);
            ms.Position = 0;

            // Read files
            using var decompBr = new BinaryReaderX(ms, true);

            var decompHeader = decompBr.ReadType<DatSubHeader>();
            if (decompHeader.fileCount > 1)
                throw new InvalidOperationException("Filecount is higher than 1. Create an issue to resolve this.");

            return new ArchiveFileInfo(new SubStream(ms, decompHeader.dataOffset, decompHeader.dataSize), "00000000.bin");
        }

        public void Save(Stream output, IArchiveFileInfo file)
        {
            var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms);
            using var outputBw = new BinaryWriterX(output);

            // Calculate offsets
            var outerDataOffset = HeaderSize;
            var innerDataOffset = (SubHeaderSize + 0x7F) & ~0x7F;

            // Write file data
            ms.Position = innerDataOffset;
            (file as ArchiveFileInfo).SaveFileData(ms);

            // Write sub header
            var subHeader = new DatSubHeader
            {
                dataOffset = innerDataOffset,
                dataSize = (int)file.FileSize,
                fileCount = 1
            };

            ms.Position = 0;
            bw.WriteType(subHeader);

            // Compress file
            ms.Position = 0;
            output.Position = outerDataOffset;
            Compressions.IrLz.Build().Compress(ms, output);

            // Write header
            var header = new DatHeader
            {
                dataOffset = outerDataOffset,
                decompSize = (int)ms.Length,
                fileSize = (int)output.Length
            };

            output.Position = 0;
            outputBw.WriteType(header);
        }
    }
}
