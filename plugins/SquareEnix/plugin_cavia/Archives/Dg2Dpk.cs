using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_cavia.Archives
{
    class Dg2Dpk
    {
        private const int Alignment_ = 0x800;
        private static readonly int EntrySize = 0x20;

        private DpkHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read entries
            input.Position = _header.entryOffset;
            var entries = ReadEntries(br, _header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.fileOffset, entry.fileSize);
                var name = $"{i:00000000}.bin";

                result.Add(new DpkArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = subStream
                }, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = _header.entryOffset;
            var fileOffset = (entryOffset + files.Count * EntrySize + Alignment_ - 1) & ~(Alignment_ - 1);

            _header.fileOffset = fileOffset;

            // Write files
            var entries = new List<DpkEntry>();

            output.Position = fileOffset;
            foreach (var file in files.Cast<DpkArchiveFile>())
            {
                var writtenSize = file.WriteFileData(output, true);
                bw.WriteAlignment(Alignment_);

                file.Entry.fileOffset = fileOffset;
                file.Entry.fileSize = (int)writtenSize;
                file.Entry.padFileSize = (int)((writtenSize + Alignment_ - 1) & ~(Alignment_ - 1));

                entries.Add(file.Entry);

                fileOffset += file.Entry.padFileSize;
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            _header.fileCount = files.Count;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private DpkHeader ReadHeader(BinaryReaderX reader)
        {
            return new DpkHeader
            {
                magic = reader.ReadString(4),
                entryOffset = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                fileOffset = reader.ReadInt32(),
                fileCount = reader.ReadInt32()
            };
        }

        private DpkEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new DpkEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private DpkEntry ReadEntry(BinaryReaderX reader)
        {
            return new DpkEntry
            {
                unk1 = reader.ReadBytes(0x10),
                fileSize = reader.ReadInt32(),
                padFileSize = reader.ReadInt32(),
                fileOffset = reader.ReadInt32(),
                zero0 = reader.ReadInt32()
            };
        }

        private void WriteHeader(DpkHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.entryOffset);
            writer.Write(header.unk1);
            writer.Write(header.fileOffset);
            writer.Write(header.fileCount);
        }

        private void WriteEntries(IList<DpkEntry> entries, BinaryWriterX writer)
        {
            foreach (DpkEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(DpkEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.unk1);
            writer.Write(entry.fileSize);
            writer.Write(entry.padFileSize);
            writer.Write(entry.fileOffset);
            writer.Write(entry.zero0);
        }
    }
}
