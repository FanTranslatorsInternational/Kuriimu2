using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_beeworks.Archives
{
    class TD1
    {
        private static readonly int EntrySize = 8;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read file count
            var fileCount = br.ReadInt32();

            // Read entries
            var entries = ReadEntries(br, fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset << 2, entry.size);
                var fileName = $"{i:00000000}.bin";

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = subStream
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = 4;
            var fileOffset = entryOffset + files.Count * EntrySize;

            // Write files
            var entries = new List<TD1Entry>();

            var filePosition = fileOffset;
            foreach (var file in files)
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);

                entries.Add(new TD1Entry
                {
                    offset = filePosition >> 2,
                    size = (int)writtenSize
                });

                filePosition += (int)((writtenSize + 3) & ~3);
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write file count
            output.Position = 0;
            bw.Write(files.Count);
        }

        private TD1Entry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new TD1Entry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private TD1Entry ReadEntry(BinaryReaderX reader)
        {
            return new TD1Entry
            {
                offset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private void WriteEntries(IList<TD1Entry> entries, BinaryWriterX writer)
        {
            foreach (TD1Entry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(TD1Entry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }
    }
}
