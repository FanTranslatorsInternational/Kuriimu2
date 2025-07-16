using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Contract.DataClasses.Plugin.File.Archive;

namespace plugin_arc_system_works.Archives
{
    class Dgkp
    {
        private static readonly int FileEntrySize = 0x90;

        private DgkpHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read entries
            br.BaseStream.Position = _header.entryOffset;
            var entries = ReadEntries(br, _header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                // There are 3 files in the game Chase: Cold Case Investigations which are 8 bytes short
                // The files are:
                // - naui/common_ui_000 - 복사본.pac
                // - naui/common_ui_000.pac
                // - naui/SearchConfirm.pac
                // This code works around this issue
                var size = entry.size;
                if (entry == entries.Last())
                    size = (int)Math.Min(input.Length - entry.offset, entry.size);

                var subStream = new SubStream(input, entry.offset, size);
                result.Add(new DgkpArchiveFile(new ArchiveFileInfo
                {
                    FilePath = entry.name.TrimEnd('\0'),
                    FileData = subStream
                }, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = _header.entryOffset;
            var fileOffset = entryOffset + files.Count * FileEntrySize;

            // Write files
            var entries = new List<DgkpFileEntry>();

            output.Position = fileOffset;
            foreach (var file in files.Cast<DgkpArchiveFile>())
            {
                fileOffset = (int)output.Position;
                var writtenSize = file.WriteFileData(output, true);

                entries.Add(new DgkpFileEntry
                {
                    offset = fileOffset,
                    size = (int)writtenSize,
                    magic = file.Entry.magic,
                    name = file.FilePath.ToRelative().FullName.PadRight(0x80, '\0')
                });
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            output.Position = 0;

            _header.fileCount = files.Count;
            WriteHeader(_header, bw);
        }

        private DgkpHeader ReadHeader(BinaryReaderX reader)
        {
            return new DgkpHeader
            {
                magic = reader.ReadString(4),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                entryOffset = reader.ReadInt32()
            };
        }

        private DgkpFileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new DgkpFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private DgkpFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new DgkpFileEntry
            {
                magic = reader.ReadString(4),
                entrySize = reader.ReadInt32(),
                size = reader.ReadInt32(),
                offset = reader.ReadInt32(),
                name = reader.ReadString(0x80)
            };
        }

        private void WriteHeader(DgkpHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.fileCount);
            writer.Write(header.entryOffset);
        }

        private void WriteEntries(IList<DgkpFileEntry> entries, BinaryWriterX writer)
        {
            foreach (DgkpFileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(DgkpFileEntry entry, BinaryWriterX writer)
        {
            writer.WriteString(entry.magic, writeNullTerminator: false);
            writer.Write(entry.entrySize);
            writer.Write(entry.size);
            writer.Write(entry.offset);
            writer.WriteString(entry.name, writeNullTerminator: false);
        }
    }
}
