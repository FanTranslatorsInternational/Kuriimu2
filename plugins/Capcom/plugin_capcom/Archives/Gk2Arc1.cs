using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using plugin_capcom.Compression;

namespace plugin_capcom.Archives
{
    class Gk2Arc1
    {
        private static readonly int EntrySize = Tools.MeasureType(typeof(Gk2Arc1Entry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read first entry
            var firstEntry = br.ReadType<Gk2Arc1Entry>();
            var fileCount = firstEntry.offset / EntrySize;

            // Read all entries
            input.Position = 0;
            var entries = br.ReadMultiple<Gk2Arc1Entry>(fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < fileCount - 1; i++)
            {
                var entry = entries[i];

                var fileSize = entry.IsCompressed ? entries[i + 1].offset - entry.offset : entry.FileSize;

                var subStream = new SubStream(input, entry.offset, fileSize);
                var fileName = $"{i:00000000}{Gk2Arc1Support.DetermineExtension(subStream, entry.IsCompressed)}";

                result.Add(CreateAfi(subStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            // Calculate offsets
            var fileOffset = (files.Count + 1) * EntrySize;

            // Write files
            var fileEntries = new List<Gk2Arc1Entry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<Gk1Arc1ArchiveFileInfo>())
            {
                output.Position = filePosition;

                var writtenSize = file.SaveFileData(output);

                file.Entry.offset = filePosition;
                file.Entry.FileSize = (int)file.FileSize;
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
            bw.WriteMultiple(fileEntries);
        }

        private IArchiveFileInfo CreateAfi(Stream file, string fileName, Gk2Arc1Entry entry)
        {
            if (!entry.IsCompressed)
                return new Gk1Arc1ArchiveFileInfo(file, fileName, entry);

            file.Position = 0;
            var compression = NintendoCompressor.PeekCompressionMethod(file);
            var decompressedSize = NintendoCompressor.PeekDecompressedSize(file);
            return new Gk1Arc1ArchiveFileInfo(file, fileName, entry, NintendoCompressor.GetConfiguration(compression), decompressedSize);
        }
    }
}
