using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_felistella.Archives
{
    class Pac
    {
        private static readonly int HeaderSize = 0x38;
        private static readonly int NameSize = 0x14;
        private static readonly int EntrySize = 0x8;

        private PacHeader _header;
        private IList<PacDirectoryEntry> _dirs;
        private IList<PacEntry> _entries;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read names
            input.Position = _header.nameOffset;
            _dirs = ReadDirectoryEntries(br, _header.nameCount);

            // Read entries
            input.Position = _header.entryOffset;
            _entries = ReadEntries(br, _header.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < _header.entryCount; i++)
            {
                var entry = _entries[i];
                if (entry is { offset: 0, size: 0 })
                    continue;

                var dirEntry = _dirs.FirstOrDefault(x => i >= x.entryIndex && i < x.entryIndex + x.entryCount);
                var name = (dirEntry?.name.Trim() ?? "") + "/" + $"{i:00000000}.bin";

                var size = entry.size * ((entry.flags & 0x100) == 0 ? 0x20 : 1);
                var fileStream = new SubStream(input, entry.offset * 0x20, size);

                result.Add(new PacArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = fileStream
                }, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var nameOffset = HeaderSize;
            var entryOffset = nameOffset + _dirs.Count * NameSize;
            var dataOffset = (entryOffset + _entries.Count * EntrySize + 0x1F) & ~0x1F;

            // Write files
            var dataPosition = dataOffset;
            foreach (var file in files.Cast<PacArchiveFile>())
            {
                // Write data
                output.Position = dataPosition;
                var writtenSize = file.WriteFileData(output, true);
                bw.WriteAlignment(0x20);

                // Update entry
                file.Entry.offset = dataPosition / 0x20;
                file.Entry.size = (int)((file.Entry.flags & 0x100) == 0 ? writtenSize / 0x20 : writtenSize);

                dataPosition += (int)((writtenSize + 0x1F) & ~0x1F);
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(_entries, bw);

            // Write dirs
            output.Position = nameOffset;
            WriteDirectoryEntries(_dirs, bw);

            // Write header
            _header.nameOffset = _dirs.Count > 0 ? nameOffset : 0;
            _header.nameCount = _dirs.Count;
            _header.entryOffset = entryOffset;
            _header.entryCount = _entries.Count;
            _header.dataSize = (int)(output.Length - 0x10);
            _header.fileSize = (int)output.Length;
            _header.blockCount = (int)(output.Length / 0x20);

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private PacHeader ReadHeader(BinaryReaderX reader)
        {
            return new PacHeader
            {
                unk1 = reader.ReadByte(),
                unk2 = reader.ReadByte(),
                pacFormat = reader.ReadByte(),
                unk3 = reader.ReadByte(),
                unk4 = reader.ReadInt32(),
                fileSize = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                unk5 = reader.ReadInt32(),
                blockCount = reader.ReadInt32(),
                nameCount = reader.ReadInt32(),
                nameOffset = reader.ReadInt32(),
                entryCount = reader.ReadInt32(),
                entryOffset = reader.ReadInt32(),
                unkCount1 = reader.ReadInt32(),
                unkOffset1 = reader.ReadInt32(),
                unkCount2 = reader.ReadInt32(),
                unkOffset2 = reader.ReadInt32()
            };
        }

        private PacDirectoryEntry[] ReadDirectoryEntries(BinaryReaderX reader, int count)
        {
            var result = new PacDirectoryEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadDirectoryEntry(reader);

            return result;
        }

        private PacDirectoryEntry ReadDirectoryEntry(BinaryReaderX reader)
        {
            return new PacDirectoryEntry
            {
                name = reader.ReadString(0x10),
                entryCount = reader.ReadInt16(),
                entryIndex = reader.ReadInt16()
            };
        }

        private PacEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new PacEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = BinaryTypeReader.Read<PacEntry>(reader)!;

            return result;
        }

        private void WriteHeader(PacHeader header, BinaryWriterX writer)
        {
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.pacFormat);
            writer.Write(header.unk3);
            writer.Write(header.unk4);
            writer.Write(header.fileSize);
            writer.Write(header.dataSize);
            writer.Write(header.unk5);
            writer.Write(header.blockCount);
            writer.Write(header.nameCount);
            writer.Write(header.nameOffset);
            writer.Write(header.entryCount);
            writer.Write(header.entryOffset);
            writer.Write(header.unkCount1);
            writer.Write(header.unkOffset1);
            writer.Write(header.unkCount2);
            writer.Write(header.unkOffset2);
        }

        private void WriteDirectoryEntries(IList<PacDirectoryEntry> entries, BinaryWriterX writer)
        {
            foreach (PacDirectoryEntry entry in entries)
                WriteDirectoryEntry(entry, writer);
        }

        private void WriteDirectoryEntry(PacDirectoryEntry entry, BinaryWriterX writer)
        {
            writer.WriteString(entry.name, writeNullTerminator: false);
            writer.Write(entry.entryCount);
            writer.Write(entry.entryIndex);
        }

        private void WriteEntries(IList<PacEntry> entries, BinaryWriterX writer)
        {
            foreach (PacEntry entry in entries)
                BinaryTypeWriter.Write(entry, writer);
        }
    }
}
