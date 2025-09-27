using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_level5.NDS.Archive
{
    class Lpck
    {
        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            LpckHeader header = ReadHeader(br);

            var result = new List<IArchiveFile>();

            for (var i = 0; i < header.fileCount; i++)
            {
                long entryPosition = input.Position;
                LpckEntry entry = ReadEntry(br);

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = entry.name,
                    FileData = new SubStream(input, entryPosition + entry.headerSize, entry.fileSize)
                }));

                input.Position = entryPosition + entry.totalSize;
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output, true);

            output.Position = 0x10;
            foreach (IArchiveFile file in files)
            {
                int headerSize = 0x10 + file.FilePath.FullName.Length + 0xF & ~0xF;
                int totalSize = headerSize + (int)file.FileSize + 0xF & ~0xF;

                if (file.FileSize % 0x10 is 0)
                    totalSize += 0x10;

                var entry = new LpckEntry
                {
                    headerSize = headerSize,
                    totalSize = totalSize,
                    fileSize = (int)file.FileSize,
                    name = file.FilePath.ToRelative().FullName
                };

                WriteEntry(entry, bw);
                bw.WriteAlignment(0x10);

                file.WriteFileData(output);
                bw.WriteAlignment(0x10);

                if (file.FileSize % 0x10 is 0)
                    bw.WritePadding(0x10);
            }

            var header = new LpckHeader
            {
                headerSize = 0x10,
                totalSize = (int)output.Length,
                fileCount = files.Count,
                magic = "LPCK"
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private LpckHeader ReadHeader(BinaryReaderX reader)
        {
            return new LpckHeader
            {
                headerSize = reader.ReadInt32(),
                totalSize = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                magic = reader.ReadString(4)
            };
        }

        private LpckEntry ReadEntry(BinaryReaderX reader)
        {
            var entry = new LpckEntry
            {
                headerSize = reader.ReadInt32(),
                totalSize = reader.ReadInt32(),
                zero = reader.ReadInt32(),
                fileSize = reader.ReadInt32(),
                name = reader.ReadNullTerminatedString()
            };

            return entry;
        }

        private void WriteHeader(LpckHeader header, BinaryWriterX writer)
        {
            writer.Write(header.headerSize);
            writer.Write(header.totalSize);
            writer.Write(header.fileCount);
            writer.WriteString(header.magic, Encoding.ASCII, writeNullTerminator: false);
        }

        private void WriteEntry(LpckEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.headerSize);
            writer.Write(entry.totalSize);
            writer.Write(entry.zero);
            writer.Write(entry.fileSize);
            writer.WriteString(entry.name, Encoding.ASCII);
        }
    }
}
