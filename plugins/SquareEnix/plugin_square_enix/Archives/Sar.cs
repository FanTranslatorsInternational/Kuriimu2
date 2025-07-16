using Komponent.IO;
using plugin_square_enix.Compression;
using System.Text;
using Komponent.Streams;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_square_enix.Archives
{
    class Sar
    {
        private static readonly int HeaderSize = 0xC;
        private static readonly int EntrySize = 0x8;

        private SarContainerHeader _header;

        public List<IArchiveFile> Load(Stream dataStream, Stream matStream)
        {
            using var br = new BinaryReaderX(dataStream, true);
            using var matBr = new BinaryReaderX(matStream);

            // Read entries
            var entries = ReadEntries(matBr, (int)(matStream.Length / EntrySize));

            // Read header
            _header = ReadContainerHeader(br);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                dataStream.Position = entry.offset;

                // Read compression header
                var compHeader = ReadContainerHeader(br);
                if (compHeader.magic != "cmp ")
                {
                    result.Add(new ArchiveFile(new ArchiveFileInfo
                    {
                        FilePath = $"{i:00000000}.bin",
                        FileData = new SubStream(dataStream, entry.offset, entry.size)
                    }));
                    continue;
                }

                compHeader = ReadContainerHeader(br);
                if (compHeader.magic != "lz7 ")
                    throw new InvalidOperationException($"Unknown compression container detected for file at index {i}.");

                var fileStream = new SubStream(dataStream, entry.offset + 0x18, SarSupport.GetCompressedSize(dataStream, entry.offset + 0x18, compHeader.data2));
                var name = $"{i:00000000}.bin";

                var compMethod = NintendoCompressor.PeekCompressionMethod(fileStream);
                result.Add(new ArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = name,
                    FileData = fileStream,
                    Compression = NintendoCompressor.GetConfiguration(compMethod),
                    DecompressedSize = NintendoCompressor.PeekDecompressedSize(fileStream)
                }));
            }

            return result;
        }

        public void Save(Stream dataStream, Stream matStream, IList<IArchiveFile> files)
        {
            long endPos;

            using var bw = new BinaryWriterX(dataStream);
            using var matBw = new BinaryWriterX(matStream);

            // Calculate offsets
            var mbrOffset = HeaderSize;
            var dataOffset = mbrOffset + HeaderSize;

            // Write files
            var entries = new List<SarEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files)
            {
                // Write file data1
                dataStream.Position = dataPosition;
                if (file.UsesCompression)
                    dataStream.Position += HeaderSize * 2;

                using var streamToWrite = new MemoryStream();
                var length = file.WriteFileData(streamToWrite);

                streamToWrite.Position = 0;
                streamToWrite.CopyTo(dataStream);

                var alignedSize = (length + 3) & ~3;

                // Write compression headers
                if (file.UsesCompression)
                {
                    endPos = dataStream.Position;
                    dataStream.Position = dataPosition;

                    WriteContainerHeader(new SarContainerHeader { magic = "cmp ", data1 = 0x00010002, data2 = (int)(alignedSize + HeaderSize * 2 + 8) }, bw);
                    // Divide bit count by 8; this may omit the remainder and is intended in the size calculation
                    // This recreates a buggy behaviour by the developers, who missed to account for the remainder properly, which can lead to the compressed size being off by 1
                    WriteContainerHeader(new SarContainerHeader { magic = "lz7 ", data1 = (int)(alignedSize + HeaderSize + 4), data2 = SarSupport.CalculateBits(streamToWrite) / 8 }, bw);

                    dataStream.Position = endPos;
                    bw.WriteString("~lz7", Encoding.ASCII, false, false);
                    bw.WriteAlignment(4);
                    bw.WriteString("~cmp", Encoding.ASCII, false, false);
                }

                // Add entry
                var entry = new SarEntry
                {
                    offset = dataPosition - dataOffset,
                    size = (int)(dataStream.Position - dataPosition)
                };
                entries.Add(entry);

                dataPosition = (int)dataStream.Position;
            }

            // Write mbr header
            endPos = dataStream.Position;

            dataStream.Position = mbrOffset;
            WriteContainerHeader(new SarContainerHeader { magic = "mbr ", data1 = (int)(dataStream.Length - HeaderSize + 4), data2 = files.Count }, bw);

            dataStream.Position = endPos;
            bw.WriteString("~mbr", Encoding.ASCII, false, false);

            // Write entries
            var mifOffset = dataStream.Position;

            dataStream.Position += HeaderSize;
            WriteEntries(entries, bw);

            // Write mif header
            endPos = dataStream.Position;

            dataStream.Position = mifOffset;
            WriteContainerHeader(new SarContainerHeader { magic = "mif ", data1 = (int)(dataStream.Length - mifOffset + 4), data2 = files.Count }, bw);

            dataStream.Position = endPos;
            bw.WriteString("~mif", Encoding.ASCII, false, false);

            // Write sar header
            bw.WriteString("~sar", Encoding.ASCII, false, false);

            dataStream.Position = 0;
            WriteContainerHeader(new SarContainerHeader { magic = "sar ", data1 = _header.data1, data2 = (int)dataStream.Length }, bw);

            // Write mat content
            foreach (var entry in entries)
                entry.offset += dataOffset; // Offsets in .mat are absolute to the .sar
            WriteEntries(entries, matBw);
        }

        private SarEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new SarEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private SarEntry ReadEntry(BinaryReaderX reader)
        {
            return new SarEntry
            {
                offset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private SarContainerHeader ReadContainerHeader(BinaryReaderX reader)
        {
            return new SarContainerHeader
            {
                magic = reader.ReadString(4),
                data1 = reader.ReadInt32(),
                data2 = reader.ReadInt32()
            };
        }

        private void WriteContainerHeader(SarContainerHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.data1);
            writer.Write(header.data2);
        }

        private void WriteEntries(IList<SarEntry> entries, BinaryWriterX writer)
        {
            foreach (SarEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(SarEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }
    }
}
