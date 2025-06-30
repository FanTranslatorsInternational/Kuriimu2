using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_inti_creates.Archives
{
    class Fnt
    {
        private static readonly int FileEntrySize = 0x8;

        public List<IArchiveFile> Load(Stream input)
        {
            var br = new BinaryReaderX(input, true);

            // Read entries
            var fileCount = br.ReadInt32();
            var entries = ReadEntries(br, fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < entries.Length; i++)
            {
                var subStream = new SubStream(input, entries[i].offset, entries[i].endOffset - entries[i].offset);
                var name = $"{i:00000000}{FntSupport.DetermineExtension(subStream)}";

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = subStream
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = (4 + files.Count * FileEntrySize + 0x7F) & ~0x7F;

            // Write files
            var entries = new List<FntFileEntry>();

            output.Position = dataOffset;
            foreach (var file in files)
            {
                var fileOffset = output.Position;
                var writtenSize = file.WriteFileData(output);
                bw.WriteAlignment(0x80);

                entries.Add(new FntFileEntry
                {
                    offset = (int)fileOffset,
                    endOffset = (int)(fileOffset + writtenSize)
                });
            }

            // Write entries
            bw.BaseStream.Position = 0;
            bw.Write(files.Count);
            WriteEntries(entries, bw);
        }

        private FntFileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new FntFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private FntFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new FntFileEntry
            {
                offset = reader.ReadInt32(),
                endOffset = reader.ReadInt32()
            };
        }

        private void WriteEntries(IList<FntFileEntry> entries, BinaryWriterX writer)
        {
            foreach (FntFileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(FntFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.endOffset);
        }
    }
}
