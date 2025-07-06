using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using plugin_capcom.Compression;

namespace plugin_capcom.Archives
{
    class Gk2Arc1
    {
        private static readonly int EntrySize = 0x8;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read first entry
            var firstEntry = ReadEntry(br);
            var fileCount = firstEntry.offset / EntrySize;

            // Read all entries
            input.Position = 0;
            var entries = ReadEntries(br, fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < fileCount - 1; i++)
            {
                var entry = entries[i];

                var fileSize = entry.IsCompressed ? (uint)(entries[i + 1].offset - entry.offset) : entry.FileSize;

                var subStream = new SubStream(input, entry.offset, fileSize);
                var fileName = $"{i:00000000}{Gk2Arc1Support.DetermineExtension(subStream, entry.IsCompressed)}";

                result.Add(CreateAfi(subStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            // Calculate offsets
            var fileOffset = (files.Count + 1) * EntrySize;

            // Write files
            var fileEntries = new List<Gk2Arc1Entry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<Gk2Arc1ArchiveFile>())
            {
                output.Position = filePosition;

                var writtenSize = file.WriteFileData(output, true);

                file.Entry.offset = filePosition;
                file.Entry.FileSize = (uint)file.FileSize;
                fileEntries.Add(file.Entry);

                filePosition += (int)((writtenSize + 3) & ~3);
            }

            fileEntries.Add(new Gk2Arc1Entry
            {
                offset = (int)output.Length
            });

            // Write entries
            using var bw = new BinaryWriterX(output);

            output.Position = 0;
            WriteEntries(fileEntries, bw);
        }

        private IArchiveFile CreateAfi(Stream file, string fileName, Gk2Arc1Entry entry)
        {
            if (!entry.IsCompressed)
                return new Gk2Arc1ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = file
                }, entry);

            file.Position = 0;
            var compression = NintendoCompressor.PeekCompressionMethod(file);
            var decompressedSize = NintendoCompressor.PeekDecompressedSize(file);
            return new Gk2Arc1ArchiveFile(new CompressedArchiveFileInfo
            {
                FilePath = fileName,
                FileData = file,
                Compression = NintendoCompressor.GetConfiguration(compression),
                DecompressedSize = decompressedSize
            }, entry);
        }

        private Gk2Arc1Entry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new Gk2Arc1Entry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private Gk2Arc1Entry ReadEntry(BinaryReaderX reader)
        {
            return new Gk2Arc1Entry
            {
                offset = reader.ReadInt32(),
                size = reader.ReadUInt32()
            };
        }

        private void WriteEntries(IList<Gk2Arc1Entry> entries, BinaryWriterX writer)
        {
            foreach (Gk2Arc1Entry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(Gk2Arc1Entry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }
    }
}
