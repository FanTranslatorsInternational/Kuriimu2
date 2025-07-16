using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_koei_tecmo.Archives
{
    class Crae
    {
        private static readonly int HeaderSize = 0x1C;
        private static readonly int EntrySize = 0x38;

        private CraeHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read entries
            input.Position = _header.entryOffset;
            var entries = ReadEntries(br, _header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.offset, entry.size);
                var fileName = entry.name.Trim('\0');

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = fileStream
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var dataOffset = entryOffset + EntrySize * files.Count;

            // Write files
            var entries = new List<CraeEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files)
            {
                // Write file data
                output.Position = dataPosition;
                file.WriteFileData(output);

                // Add entry
                entries.Add(new CraeEntry
                {
                    offset = dataPosition,
                    size = (int)file.FileSize,
                    name = file.FilePath.GetName().PadRight(0x30, '\0')
                });

                dataPosition += (int)file.FileSize;
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            _header.entrySize = dataOffset - HeaderSize;
            _header.entryOffset = entryOffset;
            _header.fileCount = files.Count;
            _header.dataSize = (int)(output.Length - dataOffset);

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private CraeHeader ReadHeader(BinaryReaderX reader)
        {
            return new CraeHeader
            {
                magic = reader.ReadString(4),
                unk1 = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                entryOffset = reader.ReadInt32(),
                entrySize = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                unk2 = reader.ReadInt32()
            };
        }

        private CraeEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new CraeEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private CraeEntry ReadEntry(BinaryReaderX reader)
        {
            return new CraeEntry
            {
                offset = reader.ReadInt32(),
                size = reader.ReadInt32(),
                name = reader.ReadString(0x30)
            };
        }

        private void WriteHeader(CraeHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.unk1);
            writer.Write(header.dataSize);
            writer.Write(header.entryOffset);
            writer.Write(header.entrySize);
            writer.Write(header.fileCount);
            writer.Write(header.unk2);
        }

        private void WriteEntries(IList<CraeEntry> entries, BinaryWriterX writer)
        {
            foreach (CraeEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(CraeEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.size);
            writer.WriteString(entry.name, writeNullTerminator: false);
        }
    }
}
