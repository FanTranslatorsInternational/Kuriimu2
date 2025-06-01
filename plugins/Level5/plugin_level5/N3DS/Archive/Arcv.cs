using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;

namespace plugin_level5.N3DS.Archive
{
    // Game: Inazuma 3 Ogre Team
    public class Arcv
    {
        private const int HeaderSize_ = 12;
        private const int EntrySize_ = 12;

        public List<ArcvArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read entries
            IList<ArcvFileInfo?> entries = ReadEntries(br, header.fileCount);

            var files = new List<ArcvArchiveFile>();
            foreach (ArcvFileInfo? entry in entries)
            {
                if (entry == null)
                    continue;

                var fileStream = new SubStream(input, entry.offset, entry.size);
                var fileInfo = new ArchiveFileInfo
                {
                    FileData = fileStream,
                    FilePath = $"{files.Count:00000000}.bin"
                };

                files.Add(new ArcvArchiveFile(fileInfo, entry));
            }

            return files;
        }

        public void Save(Stream output, List<ArcvArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            bw.BaseStream.Position = (HeaderSize_ + files.Count * EntrySize_ + 0x7F) & ~0x7F;

            // Write files
            foreach (ArcvArchiveFile file in files)
            {
                file.Entry.offset = (int)bw.BaseStream.Position;
                file.Entry.size = (int)file.FileSize;

                file.WriteFileData(bw.BaseStream, false);

                bw.WriteAlignment(0x80, 0xAC);
            }

            // Write header
            var header = new ArcvHeader
            {
                magic = "ARCV",
                fileSize = (int)output.Length,
                fileCount = files.Count
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);

            // Write file entries
            WriteEntries(files, bw);

            // Pad with 0xAC to first file
            bw.WriteAlignment(0x80, 0xAC);
        }

        private ArcvHeader ReadHeader(BinaryReaderX reader)
        {
            return new ArcvHeader
            {
                magic = reader.ReadString(4),
                fileCount = reader.ReadInt32(),
                fileSize = reader.ReadInt32()
            };
        }

        private ArcvFileInfo[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new ArcvFileInfo[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private ArcvFileInfo ReadEntry(BinaryReaderX reader)
        {
            return new ArcvFileInfo
            {
                offset = reader.ReadInt32(),
                size = reader.ReadInt32(),
                hash = reader.ReadUInt32()
            };
        }

        private void WriteHeader(ArcvHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileCount);
            writer.Write(header.fileSize);
        }

        private void WriteEntries(IList<ArcvArchiveFile> entries, BinaryWriterX writer)
        {
            foreach (ArcvArchiveFile entry in entries)
                WriteEntry(entry.Entry, writer);
        }

        private void WriteEntry(ArcvFileInfo entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.size);
            writer.Write(entry.hash);
        }
    }
}
