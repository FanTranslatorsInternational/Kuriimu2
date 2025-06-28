using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Kryptography.Checksum.Crc;

namespace plugin_ganbarion.Archives
{
    class Jarc
    {
        private static readonly int HeaderSize = 0x10;
        private static readonly int EntrySize = 0x14;

        private JarcHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read entries
            var entries = ReadEntries(br, _header.fileCount);

            // Read files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                input.Position = entry.nameOffset;

                var fileStream = new SubStream(input, entry.fileOffset, entry.fileSize);
                var name = br.ReadNullTerminatedString();

                result.Add(new JarcArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = fileStream
                }, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            var crc32 = Crc32.Crc32B;
            using var bw = new BinaryWriterX(output, true);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var nameOffset = entryOffset + files.Count * EntrySize;
            var dataOffset = (nameOffset + files.Sum(x => x.FilePath.ToRelative().FullName.Length + 1) + 0x7F) & ~0x7F;

            // Write files
            var namePosition = nameOffset;
            var dataPosition = dataOffset;

            var entries = new List<JarcEntry>();
            foreach (var file in files.Cast<JarcArchiveFile>())
            {
                output.Position = dataPosition;
                _ = file.WriteFileData(output, false);

                entries.Add(new JarcEntry
                {
                    fileOffset = dataPosition,
                    nameOffset = namePosition,
                    fileSize = (int)file.FileSize,
                    hash = crc32.ComputeValue(file.FilePath.ToRelative().FullName),
                    unk1 = file.Entry.unk1
                });

                dataPosition += (int)((file.FileSize + 0x7F) & ~0x7F);
                namePosition += Encoding.ASCII.GetByteCount(file.FilePath.ToRelative().FullName) + 1;
            }

            // Write names
            output.Position = nameOffset;
            foreach (var file in files)
                bw.WriteString(file.FilePath.ToRelative().FullName, Encoding.ASCII);

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            _header.fileCount = files.Count;
            _header.fileSize = (int)output.Length;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private JarcHeader ReadHeader(BinaryReaderX reader)
        {
            return new JarcHeader
            {
                magic = reader.ReadString(4),
                fileSize = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                fileCount = reader.ReadInt32()
            };
        }

        private JarcEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new JarcEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private JarcEntry ReadEntry(BinaryReaderX reader)
        {
            return new JarcEntry
            {
                fileOffset = reader.ReadInt32(),
                fileSize = reader.ReadInt32(),
                nameOffset = reader.ReadInt32(),
                hash = reader.ReadUInt32(),
                unk1 = reader.ReadInt32()
            };
        }

        private void WriteHeader(JarcHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileSize);
            writer.Write(header.unk1);
            writer.Write(header.fileCount);
        }

        private void WriteEntries(IList<JarcEntry> entries, BinaryWriterX writer)
        {
            foreach (JarcEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(JarcEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.fileOffset);
            writer.Write(entry.fileSize);
            writer.Write(entry.nameOffset);
            writer.Write(entry.hash);
            writer.Write(entry.unk1);
        }
    }
}
