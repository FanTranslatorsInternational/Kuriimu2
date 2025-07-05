
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using plugin_cattle_call.Compression;

namespace plugin_cattle_call.Archives
{
    class Pack
    {
        private static readonly int EntrySize = 0xC;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read file count
            var fileCount = br.ReadInt32();

            // Read entries
            var entries = ReadEntries(br, fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.offset, entry.size);
                var fileName = $"{entry.hash:X8}.bin";

                result.Add(CreateAfi(fileStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offset
            var entryOffset = 4;
            var dataOffset = entryOffset + files.Count * EntrySize;

            // Write files
            var entries = new List<PackEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files.Cast<PackArchiveFile>().OrderBy(x => x.Entry.offset))
            {
                // Write file data
                output.Position = dataPosition;
                var writtenSize = file.WriteFileData(output, true);

                // Add entry
                entries.Add(new PackEntry { offset = dataPosition, size = (int)writtenSize, hash = file.Entry.hash });

                dataPosition += (int)((writtenSize + 3) & ~3);
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries.OrderBy(x => x.hash).ToArray(), bw);

            // Write file count
            output.Position = 0;
            bw.Write(files.Count);
        }

        private IArchiveFile CreateAfi(Stream input, string fileName, PackEntry entry)
        {
            var method = NintendoCompressor.PeekCompressionMethod(input);
            if (!Enum.IsDefined(method))
                return new PackArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = input
                }, entry);

            return new PackArchiveFile(new CompressedArchiveFileInfo
            {
                FilePath = fileName,
                FileData = input,
                Compression = NintendoCompressor.GetConfiguration(method),
                DecompressedSize = NintendoCompressor.PeekDecompressedSize(input)
            }, entry);
        }

        private PackEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new PackEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private PackEntry ReadEntry(BinaryReaderX reader)
        {
            return new PackEntry
            {
                hash = reader.ReadUInt32(),
                offset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private void WriteEntries(IList<PackEntry> entries, BinaryWriterX writer)
        {
            foreach (PackEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(PackEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.hash);
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }
    }
}
