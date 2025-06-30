using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_inti_creates.Archives
{
    class Dat
    {
        private static int HeaderSize = 0x18;
        private static int SubHeaderSize = 0x14;

        public IArchiveFile Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            var header = ReadHeader(br);

            // Decompress file
            var ms = new MemoryStream();
            Compressions.IrLz.Build().Decompress(new SubStream(input, header.dataOffset, header.fileSize - header.dataOffset), ms);
            ms.Position = 0;

            // Read files
            using var decompBr = new BinaryReaderX(ms, true);

            var decompHeader = ReadSubHeader(decompBr);
            if (decompHeader.fileCount > 1)
                throw new InvalidOperationException("Filecount is higher than 1. Create an issue to resolve this.");

            return new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = "00000000.bin",
                FileData = new SubStream(ms, decompHeader.dataOffset, decompHeader.dataSize)
            });
        }

        public void Save(Stream output, IArchiveFile file)
        {
            var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms);
            using var outputBw = new BinaryWriterX(output);

            // Calculate offsets
            var outerDataOffset = HeaderSize;
            var innerDataOffset = (SubHeaderSize + 0x7F) & ~0x7F;

            // Write file data
            ms.Position = innerDataOffset;
            file.WriteFileData(ms);

            // Write sub header
            var subHeader = new DatSubHeader
            {
                dataOffset = innerDataOffset,
                dataSize = (int)file.FileSize,
                fileCount = 1
            };

            ms.Position = 0;
            WriteSubHeader(subHeader, bw);

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
            WriteHeader(header, outputBw);
        }

        private DatHeader ReadHeader(BinaryReaderX reader)
        {
            return new DatHeader
            {
                dataOffset = reader.ReadInt32(),
                zero1 = reader.ReadInt32(),
                fileSize = reader.ReadInt32(),
                decompSize = reader.ReadInt32(),
                zero2 = reader.ReadInt32(),
                zero3 = reader.ReadInt32()
            };
        }

        private DatSubHeader ReadSubHeader(BinaryReaderX reader)
        {
            return new DatSubHeader
            {
                fileCount = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                zero1 = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                dataSize = reader.ReadInt32()
            };
        }

        private void WriteHeader(DatHeader header, BinaryWriterX writer)
        {
            writer.Write(header.dataOffset);
            writer.Write(header.zero1);
            writer.Write(header.fileSize);
            writer.Write(header.decompSize);
            writer.Write(header.zero2);
            writer.Write(header.zero3);
        }

        private void WriteSubHeader(DatSubHeader header, BinaryWriterX writer)
        {
            writer.Write(header.fileCount);
            writer.Write(header.unk1);
            writer.Write(header.zero1);
            writer.Write(header.dataOffset);
            writer.Write(header.dataSize);
        }
    }
}
