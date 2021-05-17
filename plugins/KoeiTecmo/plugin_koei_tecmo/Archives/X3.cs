using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_koei_tecmo.Archives
{
    class X3
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(X3Header));
        private static readonly int EntrySize = Tools.MeasureType(typeof(X3FileEntry));

        private X3Header _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<X3Header>();

            // Read file entries
            var entries = br.ReadMultiple<X3FileEntry>(_header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
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
                result.Add(new X3ArchiveFileInfo(rawFileStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var alignment = 0x20;
            var entryOffset = HeaderSize;
            var dataOffset = (entryOffset + files.Count * EntrySize + 0x1F) & ~0x1F;

            // Write files
            var entries = new List<X3FileEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files.Cast<X3ArchiveFileInfo>())
            {
                output.Position = dataPosition;

                // Write file data
                var finalStream = file.GetFinalStream();
                finalStream.CopyTo(output);
                bw.WriteAlignment(alignment, 0xCD);

                // Update entry
                file.Entry.offset = dataPosition / alignment;
                file.Entry.fileSize = (int)finalStream.Length;
                file.Entry.decompressedFileSize = file.ShouldCompress? (int)file.FileSize : 0;

                entries.Add(file.Entry);
                dataPosition = (int)((dataPosition + finalStream.Length + 0x1F) & ~0x1F);
            }

            // Write file entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            output.Position = 0;
            bw.WriteType(new X3Header { fileCount = files.Count, alignment = alignment });
        }
    }
}
