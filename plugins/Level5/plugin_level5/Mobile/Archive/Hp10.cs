using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Extensions;
using Kryptography.Checksum.Crc;

namespace plugin_level5.Mobile.Archive
{
    public class Hp10
    {
        private const int HeaderSize_ = 32;
        private const int EntrySize_ = 32;

        private Hp10Header _header;

        public List<Hp10ArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read entries
            var entries = ReadEntries(br, _header.fileCount);

            // Add files
            var result = new List<Hp10ArchiveFile>();
            foreach (var entry in entries)
            {
                input.Position = _header.stringOffset + entry.nameOffset;
                var name = br.ReadNullTerminatedString();
                var fileStream = new SubStream(input, _header.dataOffset + entry.fileOffset, entry.fileSize);

                var fileInfo = new ArchiveFileInfo
                {
                    FileData = fileStream,
                    FilePath = name
                };
                result.Add(new Hp10ArchiveFile(fileInfo, entry));
            }

            return result;
        }

        public void Save(Stream output, List<Hp10ArchiveFile> files)
        {
            var crc32b = Crc32.Crc32B;
            var crc32c = Crc32.Crc32C;

            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize_;
            var stringOffset = entryOffset + files.Count * EntrySize_;
            var dataOffset = (stringOffset + files.Sum(x => x.FilePath.ToRelative().FullName.Length + 1) + 0x7FF) & ~0x7FF;

            // Write files
            var dataPosition = (long)dataOffset;
            var namePosition = 0;

            var entries = new List<Hp10FileEntry>();
            var strings = new List<string>();
            foreach (Hp10ArchiveFile file in files)
            {
                // Write file data
                output.Position = dataPosition;
                var writtenSize = file.WriteFileData(output, false);
                bw.WriteAlignment(0x800);

                // Update entry
                file.Entry.crc32bFileNameHash = crc32b.ComputeValue(file.FilePath.GetName());
                file.Entry.crc32cFileNameHash = crc32c.ComputeValue(file.FilePath.GetName());
                file.Entry.crc32bFilePathHash = crc32b.ComputeValue(file.FilePath.ToRelative().FullName);
                file.Entry.crc32cFilePathHash = crc32c.ComputeValue(file.FilePath.ToRelative().FullName);
                file.Entry.nameOffset = namePosition;
                file.Entry.fileOffset = (uint)(dataPosition - dataOffset);
                file.Entry.fileSize = (int)writtenSize;

                entries.Add(file.Entry);
                strings.Add(file.FilePath.ToRelative().FullName);

                // Update positions
                namePosition += file.FilePath.ToRelative().FullName.Length + 1;
                dataPosition = (dataPosition + writtenSize + 0x7FF) & ~0x7FF;
            }

            // Write strings
            output.Position = stringOffset;
            foreach (var name in strings)
                bw.WriteString(name);
            var stringEndOffset = output.Position;

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            _header.dataOffset = dataOffset;
            _header.stringOffset = stringOffset;
            _header.fileCount = files.Count;
            _header.fileSize = (uint)output.Length;
            _header.stringEnd = (int)stringEndOffset;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private Hp10Header ReadHeader(BinaryReaderX reader)
        {
            return new Hp10Header
            {
                magic = reader.ReadString(4),
                fileCount = reader.ReadInt32(),
                fileSize = reader.ReadUInt32(),
                stringEnd = reader.ReadInt32(),
                stringOffset = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                unk1 = reader.ReadInt16(),
                unk2 = reader.ReadInt16(),
                zero1 = reader.ReadInt32()
            };
        }

        private Hp10FileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new Hp10FileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private Hp10FileEntry ReadEntry(BinaryReaderX reader)
        {
            return new Hp10FileEntry
            {
                crc32bFileNameHash = reader.ReadUInt32(),
                crc32cFileNameHash = reader.ReadUInt32(),
                crc32bFilePathHash = reader.ReadUInt32(),
                crc32cFilePathHash = reader.ReadUInt32(),
                fileOffset = reader.ReadUInt32(),
                fileSize = reader.ReadInt32(),
                nameOffset = reader.ReadInt32(),
                timestamp = reader.ReadUInt32()
            };
        }

        private void WriteHeader(Hp10Header header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileCount);
            writer.Write(header.fileSize);
            writer.Write(header.stringEnd);
            writer.Write(header.stringOffset);
            writer.Write(header.dataOffset);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.zero1);
        }

        private void WriteEntries(IList<Hp10FileEntry> entries, BinaryWriterX writer)
        {
            foreach (Hp10FileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(Hp10FileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.crc32bFileNameHash);
            writer.Write(entry.crc32cFileNameHash);
            writer.Write(entry.crc32bFilePathHash);
            writer.Write(entry.crc32cFilePathHash);
            writer.Write(entry.fileOffset);
            writer.Write(entry.fileSize);
            writer.Write(entry.nameOffset);
            writer.Write(entry.timestamp);
        }
    }
}
