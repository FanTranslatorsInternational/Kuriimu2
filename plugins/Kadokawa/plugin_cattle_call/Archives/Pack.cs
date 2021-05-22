using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using plugin_cattle_call.Compression;

namespace plugin_cattle_call.Archives
{
    class Pack
    {
        private static readonly int EntrySize = Tools.MeasureType(typeof(PackEntry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read file count
            var fileCount = br.ReadInt32();

            // Read entries
            var entries = br.ReadMultiple<PackEntry>(fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.offset, entry.size);
                var fileName = $"{entry.hash:X8}.bin";

                result.Add(CreateAfi(fileStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offset
            var entryOffset = 4;
            var dataOffset = entryOffset + files.Count * EntrySize;

            // Write files
            var entries = new List<PackEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files.Cast<PackArchiveFileInfo>().OrderBy(x => x.Entry.offset))
            {
                // Write file data
                output.Position = dataPosition;
                var writtenSize = file.SaveFileData(output);

                // Add entry
                entries.Add(new PackEntry { offset = dataPosition, size = (int)writtenSize, hash = file.Entry.hash });

                dataPosition += (int)((writtenSize + 3) & ~3);
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries.OrderBy(x => x.hash));

            // Write file count
            output.Position = 0;
            bw.Write(files.Count);
        }

        private IArchiveFileInfo CreateAfi(Stream input, string fileName, PackEntry entry)
        {
            var method = NintendoCompressor.PeekCompressionMethod(input);
            if (!NintendoCompressor.IsValidCompressionMethod(method))
                return new PackArchiveFileInfo(input, fileName, entry);

            return new PackArchiveFileInfo(input, fileName, entry, NintendoCompressor.GetConfiguration(method), NintendoCompressor.PeekDecompressedSize(input));
        }
    }
}
