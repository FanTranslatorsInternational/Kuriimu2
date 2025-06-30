using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_hunex.Archives
{
    // Specifications: https://github.com/Hintay/PS-HuneX_Tools/tree/master/Specifications
    class MRG
    {
        private static readonly int HeaderSize = 0x8;
        private static readonly int EntrySize = 0x8;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read entries
            var entries = ReadEntries(br, header.fileCount);

            // Add files
            var dataOffset = br.BaseStream.Position;

            var result = new List<IArchiveFile>();
            for (var i = 0; i < header.fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, dataOffset + entry.Offset, entry.Size);
                var fileName = $"{i:00000000}.bin";

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = subStream
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = entryOffset + files.Count * EntrySize;

            // Write files
            var entries = new List<MRGEntry>();

            var filePosition = fileOffset;
            foreach (var file in files)
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);
                bw.WritePadding(8, 0xFF);

                entries.Add(new MRGEntry
                {
                    Offset = filePosition - fileOffset,
                    Size = (int)writtenSize
                });

                filePosition += (int)writtenSize + 8;
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            var header = new MRGHeader
            {
                fileCount = (short)files.Count
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private MRGHeader ReadHeader(BinaryReaderX reader)
        {
            return new MRGHeader
            {
                magic = reader.ReadString(6),
                fileCount = reader.ReadInt16()
            };
        }

        private MRGEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new MRGEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private MRGEntry ReadEntry(BinaryReaderX reader)
        {
            return new MRGEntry
            {
                sectorOffset = reader.ReadUInt16(),
                lowOffset = reader.ReadUInt16(),
                sectorCount = reader.ReadUInt16(),
                lowSize = reader.ReadUInt16()
            };
        }

        private void WriteHeader(MRGHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileCount);
        }

        private void WriteEntries(IList<MRGEntry> entries, BinaryWriterX writer)
        {
            foreach (MRGEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(MRGEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.sectorOffset);
            writer.Write(entry.lowOffset);
            writer.Write(entry.sectorCount);
            writer.Write(entry.lowSize);
        }
    }
}
