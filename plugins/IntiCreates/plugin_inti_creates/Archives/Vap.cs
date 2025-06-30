using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_inti_creates.Archives
{
    class Vap
    {
        private static readonly int HeaderSize = 0xC;
        private static readonly int FileEntrySize = 0x10;

        private VapHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read entries
            var entries = ReadEntries(br, _header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entry.size);
                var name = $"{i:00000000}{VapSupport.DetermineExtension(subStream)}";

                result.Add(new VapArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = subStream,
                }, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileOffset = (HeaderSize + files.Count * FileEntrySize + 0x7F) & ~0x7F;

            // Write files
            bw.BaseStream.Position = fileOffset;

            var entries = new List<VapFileEntry>();
            foreach (var file in files.Cast<VapArchiveFile>())
            {
                fileOffset = (int)bw.BaseStream.Position;
                var writtenSize = file.WriteFileData(output, true);

                if (file != files.Last())
                    bw.WriteAlignment(0x80);

                entries.Add(new VapFileEntry
                {
                    offset = fileOffset,
                    size = (int)writtenSize,

                    unk1 = file.Entry.unk1,
                    unk2 = file.Entry.unk2
                });
            }

            // Write entries
            bw.BaseStream.Position = HeaderSize;
            WriteEntries(entries, bw);

            // Write header
            var header = new VapHeader
            {
                fileCount = files.Count,
                unk1 = _header.unk1
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);
        }

        private VapHeader ReadHeader(BinaryReaderX reader)
        {
            return new VapHeader
            {
                fileCount = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                zero0 = reader.ReadInt32()
            };
        }

        private VapFileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new VapFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private VapFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new VapFileEntry
            {
                offset = reader.ReadInt32(),
                size = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32()
            };
        }

        private void WriteHeader(VapHeader header, BinaryWriterX writer)
        {
            writer.Write(header.fileCount);
            writer.Write(header.unk1);
            writer.Write(header.zero0);
        }

        private void WriteEntries(IList<VapFileEntry> entries, BinaryWriterX writer)
        {
            foreach (VapFileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(VapFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.size);
            writer.Write(entry.unk1);
            writer.Write(entry.unk2);
        }
    }
}
