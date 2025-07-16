using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_level5.NDS.Archive
{
    // Game: Professor Layton 3 on DS
    public class Lpc2
    {
        private const int HeaderSize_ = 0x1C;
        private const int FileEntrySize_ = 0xC;

        public List<ArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read file entries
            br.BaseStream.Position = header.fileEntryOffset;
            var entries = ReadEntries(br, header.fileCount);

            // Add files
            var result = new List<ArchiveFile>();
            foreach (var entry in entries)
            {
                br.BaseStream.Position = header.nameOffset + entry.nameOffset;
                var name = br.ReadNullTerminatedString();

                var fileStream = new SubStream(input, header.dataOffset + entry.fileOffset, entry.fileSize);

                var fileInfo = new ArchiveFileInfo
                {
                    FileData = fileStream,
                    FilePath = name,
                    PluginIds = Lpc2Support.RetrievePluginMapping(name)
                };
                result.Add(new ArchiveFile(fileInfo));
            }

            return result;
        }

        public void Save(Stream output, IList<ArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            var fileEntryStartOffset = (HeaderSize_ + 0xF) & ~0xF;
            var nameStartOffset = fileEntryStartOffset + files.Count * FileEntrySize_;

            // Write names
            var fileOffset = 0;
            var nameOffset = 0;
            var fileEntries = new List<Lpc2FileEntry>();
            foreach (var file in files)
            {
                fileEntries.Add(new Lpc2FileEntry
                {
                    fileOffset = fileOffset,
                    fileSize = (int)file.FileSize,
                    nameOffset = nameOffset
                });

                bw.BaseStream.Position = nameStartOffset + nameOffset;
                bw.WriteString(file.FilePath.ToRelative().FullName, Encoding.ASCII);
                nameOffset = (int)bw.BaseStream.Position - nameStartOffset;

                fileOffset += (int)file.FileSize;

                if (file.FileSize % 4 == 0)
                    fileOffset += 4;
                else
                    fileOffset = (fileOffset + 3) & ~3;
            }

            // Write file data
            var dataOffset = (int)((bw.BaseStream.Position + 3) & ~3);
            bw.BaseStream.Position = dataOffset;

            foreach (var file in files)
            {
                file.WriteFileData(bw.BaseStream, false);

                if (file.FileSize % 4 == 0)
                    bw.WritePadding(4);
                else
                    bw.WriteAlignment(4);
            }

            // Write file entries
            bw.BaseStream.Position = fileEntryStartOffset;
            WriteEntries(fileEntries, bw);

            // Write header
            var header = new Lpc2Header
            {
                magic = "LPC2",

                fileEntryOffset = fileEntryStartOffset,
                nameOffset = nameStartOffset,
                dataOffset = dataOffset,

                fileCount = files.Count,

                headerSize = dataOffset,
                fileSize = (int)bw.BaseStream.Length
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);
        }

        private Lpc2Header ReadHeader(BinaryReaderX reader)
        {
            return new Lpc2Header
            {
                magic = reader.ReadString(4),
                fileCount = reader.ReadInt32(),
                headerSize = reader.ReadInt32(),
                fileSize = reader.ReadInt32(),
                fileEntryOffset = reader.ReadInt32(),
                nameOffset = reader.ReadInt32(),
                dataOffset = reader.ReadInt32()
            };
        }

        private Lpc2FileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new Lpc2FileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private Lpc2FileEntry ReadEntry(BinaryReaderX reader)
        {
            return new Lpc2FileEntry
            {
                nameOffset = reader.ReadInt32(),
                fileOffset = reader.ReadInt32(),
                fileSize = reader.ReadInt32()
            };
        }

        private void WriteHeader(Lpc2Header header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileCount);
            writer.Write(header.headerSize);
            writer.Write(header.fileSize);
            writer.Write(header.fileEntryOffset);
            writer.Write(header.nameOffset);
            writer.Write(header.dataOffset);
        }

        private void WriteEntries(IList<Lpc2FileEntry> entries, BinaryWriterX writer)
        {
            foreach (Lpc2FileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(Lpc2FileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.nameOffset);
            writer.Write(entry.fileOffset);
            writer.Write(entry.fileSize);
        }
    }
}
