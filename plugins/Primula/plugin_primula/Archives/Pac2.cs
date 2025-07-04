using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_primula.Archives
{
    class Pac2
    {
        private static readonly int HeaderSize = 0x10;
        private static readonly int EntrySize = 0x8;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read entries
            var fileNames = new List<string>();
            for (int i = 0; i < header.fileCount; i++)
            {
                fileNames.Add(Encoding.ASCII.GetString(br.ReadBytes(0x20)).Trim('\0'));
            }

            var entries = ReadEntries(br, header.fileCount);
            var dataOrigin = input.Position;

            // Add files
            var result = new List<IArchiveFile>();
            for (int i = 0; i < header.fileCount; i++)
            {
                var name = fileNames[i];
                var fileStream = new SubStream(input, entries[i].Position + dataOrigin, entries[i].Size);

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = fileStream
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileNameOffset = HeaderSize;
            var pointerOffset = fileNameOffset + 0x20 * files.Count;
            var dataOffset = pointerOffset + EntrySize * files.Count;

            // Write header
            var header = new Pac2Header { fileCount = files.Count };
            WriteHeader(header, bw);

            // Write filenames
            foreach (var file in files)
            {
                var fileName = Encoding.ASCII.GetBytes(file.FilePath.ToString().TrimStart('/'));
                bw.Write(fileName);
                bw.WritePadding(0x20 - fileName.Length);
            }

            // Write files
            var entries = new List<Pac2Entry>();

            output.Position = dataOffset;
            var basePos = 0;
            foreach (var file in files)
            {
                var writtenSize = file.WriteFileData(output);

                entries.Add(new Pac2Entry
                {
                    Position = basePos,
                    Size = (int)writtenSize
                });

                basePos += (int)writtenSize;
            }

            // Write pointers
            output.Position = pointerOffset;
            WriteEntries(entries, bw);
        }

        private Pac2Header ReadHeader(BinaryReaderX reader)
        {
            return new Pac2Header
            {
                magic = reader.ReadString(12),
                fileCount = reader.ReadInt32()
            };
        }

        private Pac2Entry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new Pac2Entry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private Pac2Entry ReadEntry(BinaryReaderX reader)
        {
            return new Pac2Entry
            {
                Position = reader.ReadInt32(),
                Size = reader.ReadInt32()
            };
        }

        private void WriteHeader(Pac2Header header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileCount);
        }

        private void WriteEntries(IList<Pac2Entry> entries, BinaryWriterX writer)
        {
            foreach (Pac2Entry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(Pac2Entry entry, BinaryWriterX writer)
        {
            writer.Write(entry.Position);
            writer.Write(entry.Size);
        }
    }
}
