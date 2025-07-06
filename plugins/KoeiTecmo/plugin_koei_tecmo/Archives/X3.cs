using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_koei_tecmo.Archives
{
    class X3
    {
        private static readonly int HeaderSize = 0x10;
        private static readonly int EntrySize = 0x10;

        private X3Header _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read file entries
            var entries = ReadEntries(br, _header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var rawFileStream = new SubStream(input, entry.offset * _header.alignment, entry.fileSize);

                // Prepare (de-)compressed file stream for extension detection
                Stream fileStream = rawFileStream;
                if (entry.IsCompressed)
                    fileStream = new X3CompressedStream(fileStream);

                var extension = X3Support.DetermineExtension(fileStream);
                var fileName = $"{result.Count:00000000}{extension}";

                // Pass unmodified SubStream, so X3Afi can take care of compression wrapping again
                // Necessary for access to original compressed file data in saving
                result.Add(new X3ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = entry.IsCompressed ? new X3CompressedStream(rawFileStream) : rawFileStream
                }, rawFileStream, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var alignment = 0x20;
            var entryOffset = HeaderSize;
            var dataOffset = (entryOffset + files.Count * EntrySize + 0x1F) & ~0x1F;

            // Write files
            var entries = new List<X3FileEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files.Cast<X3ArchiveFile>())
            {
                output.Position = dataPosition;

                // Write file data
                var finalStream = file.GetFinalStream();
                finalStream.CopyTo(output);
                bw.WriteAlignment(alignment, 0xCD);

                // Update entry
                file.Entry.offset = dataPosition / alignment;
                file.Entry.fileSize = (int)finalStream.Length;
                file.Entry.decompressedFileSize = file.ShouldCompress ? (int)file.FileSize : 0;

                entries.Add(file.Entry);
                dataPosition = (int)((dataPosition + finalStream.Length + 0x1F) & ~0x1F);
            }

            // Write file entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            var header = new X3Header
            {
                fileCount = files.Count,
                alignment = alignment
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private X3Header ReadHeader(BinaryReaderX reader)
        {
            return new X3Header
            {
                magic = reader.ReadUInt32(),
                fileCount = reader.ReadInt32(),
                alignment = reader.ReadInt32(),
                zero0 = reader.ReadInt32()
            };
        }

        private X3FileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new X3FileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private X3FileEntry ReadEntry(BinaryReaderX reader)
        {
            return new X3FileEntry
            {
                offset = reader.ReadInt64(),
                fileSize = reader.ReadInt32(),
                decompressedFileSize = reader.ReadInt32()
            };
        }

        private void WriteHeader(X3Header header, BinaryWriterX writer)
        {
            writer.Write(header.magic);
            writer.Write(header.fileCount);
            writer.Write(header.alignment);
            writer.Write(header.zero0);
        }

        private void WriteEntries(IList<X3FileEntry> entries, BinaryWriterX writer)
        {
            foreach (X3FileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(X3FileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.fileSize);
            writer.Write(entry.decompressedFileSize);
        }
    }
}
