using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_nippon_ichi.Archives
{
    class Dat
    {
        private static readonly int HeaderSize = 0x10;
        private static readonly int EntrySize = 0x2C;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read entries
            var entries = ReadEntries(br, header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.offset, entry.size);
                var fileName = entry.name.Trim('\0');

                result.Add(new DatArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = fileStream
                }, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var dataOffset = (entryOffset + files.Count * EntrySize + 0x7FF) & ~0x7FF;

            // Write files
            var entries = new List<DatEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files.Cast<DatArchiveFile>())
            {
                // Write file data
                output.Position = dataPosition;
                file.WriteFileData(output, true);

                // Add entry
                entries.Add(new DatEntry
                {
                    offset = dataPosition,
                    size = (int)file.FileSize,
                    name = file.FilePath.GetName().PadRight(0x20, '\0'),
                    unk1 = file.Entry.unk1
                });

                dataPosition = (int)((dataPosition + file.FileSize + 0x7FF) & ~0x7FF);
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            output.Position = 0;
            WriteHeader(new DatHeader { fileCount = files.Count }, bw);
        }

        private DatHeader ReadHeader(BinaryReaderX reader)
        {
            return new DatHeader
            {
                magic = reader.ReadString(8),
                zero0 = reader.ReadInt32(),
                fileCount = reader.ReadInt32()
            };
        }

        private DatEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new DatEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private DatEntry ReadEntry(BinaryReaderX reader)
        {
            return new DatEntry
            {
                name = reader.ReadString(0x20),
                offset = reader.ReadInt32(),
                size = reader.ReadInt32(),
                unk1 = reader.ReadUInt32()
            };
        }

        private void WriteHeader(DatHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.zero0);
            writer.Write(header.fileCount);
        }

        private void WriteEntries(IList<DatEntry> entries, BinaryWriterX writer)
        {
            foreach (DatEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(DatEntry entry, BinaryWriterX writer)
        {
            writer.WriteString(entry.name, writeNullTerminator: false);
            writer.Write(entry.offset);
            writer.Write(entry.size);
            writer.Write(entry.unk1);
        }
    }
}
