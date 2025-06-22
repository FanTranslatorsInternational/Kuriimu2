using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    class UMSBT
    {
        private const int EntrySize = 0x8;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read first offset
            var firstOffset = br.ReadInt32();

            // Read entries
            input.Position = 0;
            var entries = ReadEntries(br, firstOffset);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entry.size);
                var fileName = $"{i:00000000}.msbt";

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = subStream
                }));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileOffset = Math.Max(0x30, (files.Count * EntrySize + 0xF) & ~0xF);

            // Write files
            var entries = new List<UMSBTEntry>();

            var filePosition = fileOffset;
            foreach (var file in files)
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);

                var entry = new UMSBTEntry
                {
                    offset = filePosition,
                    size = (int)writtenSize
                };
                entries.Add(entry);

                filePosition += (int)writtenSize;
            }

            // Write entries
            output.Position = 0;
            WriteEntries(entries,bw);
        }

        private UMSBTEntry[] ReadEntries(BinaryReaderX reader, int endOffset)
        {
            int count = (endOffset - (int)reader.BaseStream.Position) / 8;
            var result = new List<UMSBTEntry>(count);

            while (reader.BaseStream.Position < endOffset)
            {
                UMSBTEntry entry = ReadEntry(reader);
                if (entry.size <= 0)
                    continue;

                result.Add(entry);
            }

            return [.. result];
        }

        private UMSBTEntry ReadEntry(BinaryReaderX reader)
        {
            return new UMSBTEntry
            {
                offset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private void WriteEntries(IList<UMSBTEntry> entries, BinaryWriterX writer)
        {
            foreach (UMSBTEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(UMSBTEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }
    }
}
