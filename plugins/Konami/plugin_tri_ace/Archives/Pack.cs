using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_tri_ace.Archives
{
    // Game: Beyond The Labyrinth
    class Pack
    {
        private static readonly int HeaderSize = 0x8;
        private static readonly int FileEntrySize = 0x10;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read entries
            var entries = ReadEntries(br, header.fileCount + 1);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < header.fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entries[i + 1].offset - entry.offset);
                var name = $"{i:00000000}{PackSupport.DetermineExtension(entry.fileType)}";

                result.Add(new PackArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = subStream,
                    PluginIds = PackSupport.RetrievePluginMapping(entries[i].fileType)
                }, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = (entryOffset + (files.Count + 1) * FileEntrySize + 0x7F) & ~0x7F;

            // Write files
            output.Position = fileOffset;

            var entries = new List<PackFileEntry>();
            foreach (var file in files.Cast<PackArchiveFile>())
            {
                fileOffset = (int)output.Position;
                file.WriteFileData(output, true);

                bw.WriteAlignment(0x10);

                entries.Add(new PackFileEntry
                {
                    offset = fileOffset,
                    fileType = file.Entry.fileType,
                    unk0 = file.Entry.unk0
                });
            }

            // Write end file/blob
            entries.Add(new PackFileEntry
            {
                offset = (int)output.Position
            });
            bw.WriteAlignment(0x400);

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            var header = new PackHeader
            {
                fileCount = (short)files.Count
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private PackHeader ReadHeader(BinaryReaderX reader)
        {
            return new PackHeader
            {
                magic = reader.ReadString(4),
                version = reader.ReadInt16(),
                fileCount = reader.ReadInt16()
            };
        }

        private PackFileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new PackFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private PackFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new PackFileEntry
            {
                offset = reader.ReadInt32(),
                fileType = reader.ReadInt32(),
                unk0 = reader.ReadInt32(),
                zero0 = reader.ReadInt32()
            };
        }

        private void WriteHeader(PackHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.fileCount);
        }

        private void WriteEntries(IList<PackFileEntry> entries, BinaryWriterX writer)
        {
            foreach (PackFileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(PackFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.fileType);
            writer.Write(entry.unk0);
            writer.Write(entry.zero0);
        }
    }
}
