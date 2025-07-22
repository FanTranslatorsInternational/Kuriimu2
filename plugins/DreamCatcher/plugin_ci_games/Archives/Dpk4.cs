using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_ci_games.Archives
{
    public class Dpk4
    {
        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            var header = ReadHeader(br);
            var entries = ReadEntries(br, header.fileCount);

            return entries.Select(e => CreateAfi(new SubStream(input, e.offset, e.compressedSize), e)).ToList();
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Jump to initial file offset
            bw.BaseStream.Position = 0x10 + files.Aggregate(0, (offset, file) => (offset + 16 + file.FilePath.ToRelative().FullName.Length + 1 + 3) & ~3);

            // Write files
            var entries = new List<Dpk4FileEntry>();
            foreach (var file in files)
            {
                var path = file.FilePath.ToRelative().FullName.Replace("/", "\\");
                var fileOffset = (int)bw.BaseStream.Position;
                var writtenSize = file.WriteFileData(bw.BaseStream);

                entries.Add(new Dpk4FileEntry
                {
                    entrySize = (16 + path.Length + 1 + 3) & ~3,
                    size = (int)file.FileSize,
                    compressedSize = (int)writtenSize,
                    offset = fileOffset,
                    fileName = path + "\0"
                });
            }

            // Create header
            var header = new Dpk4Header
            {
                fileSize = (uint)bw.BaseStream.Position
            };

            // Write entries
            bw.BaseStream.Position = 0x10;
            WriteEntries(entries, bw);
            header.fileTableSize = (int)bw.BaseStream.Position - 0x10;
            header.fileCount = entries.Count;

            // Header
            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);
        }

        private IArchiveFile CreateAfi(Stream file, Dpk4FileEntry entry)
        {
            if (entry.IsCompressed)
                return new ArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = entry.fileName.Trim('\0'),
                    FileData = file,
                    Compression = Compressions.ZLib.Build(),
                    DecompressedSize = entry.size
                });

            return new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = entry.fileName.Trim('\0'),
                FileData = file
            });
        }

        private Dpk4Header ReadHeader(BinaryReaderX reader)
        {
            return new Dpk4Header
            {
                magic = reader.ReadString(4),
                fileSize = reader.ReadUInt32(),
                fileTableSize = reader.ReadInt32(),
                fileCount = reader.ReadInt32()
            };
        }

        private Dpk4FileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new Dpk4FileEntry[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ReadEntry(reader);
                reader.SeekAlignment(4);
            }

            return result;
        }

        private Dpk4FileEntry ReadEntry(BinaryReaderX reader)
        {
            var entry = new Dpk4FileEntry
            {
                entrySize = reader.ReadInt32(),
                size = reader.ReadInt32(),
                compressedSize = reader.ReadInt32(),
                offset = reader.ReadInt32()
            };

            entry.fileName = reader.ReadString(entry.entrySize - 0x10);

            return entry;
        }

        private void WriteHeader(Dpk4Header header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileSize);
            writer.Write(header.fileTableSize);
            writer.Write(header.fileCount);
        }

        private void WriteEntries(IList<Dpk4FileEntry> entries, BinaryWriterX writer)
        {
            foreach (Dpk4FileEntry entry in entries)
            {
                WriteEntry(entry, writer);
                writer.WriteAlignment(4);
            }
        }

        private void WriteEntry(Dpk4FileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.entrySize);
            writer.Write(entry.size);
            writer.Write(entry.compressedSize);
            writer.Write(entry.offset);
            writer.WriteString(entry.fileName, writeNullTerminator: false);
        }
    }
}
