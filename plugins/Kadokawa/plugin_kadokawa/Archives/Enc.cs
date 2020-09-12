using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_kadokawa.Archives
{
    // TODO: Add CTX Image Guid
    class Enc
    {
        private static readonly int FileEntrySize = Tools.MeasureType(typeof(EncFileEntry));

        public IList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read entries
            var entryCount = br.ReadInt32();
            var entries = br.ReadMultiple<EncFileEntry>(entryCount);

            // Add files
            var result = new List<ArchiveFileInfo>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entry.size & 0x7FFFFFFF);
                var name = $"{i:00000000}{EncSupport.DetermineExtension(subStream)}";

                result.Add(CreateAfi(subStream, name, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<ArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileOffset = (4 + files.Count * FileEntrySize + 0x3F) & ~0x3F;

            // Write files
            var entries = new List<EncFileEntry>();

            bw.BaseStream.Position = fileOffset;
            foreach (var file in files)
            {
                fileOffset = (int)bw.BaseStream.Position;
                var writtenSize = file.SaveFileData(output);

                if (file != files.Last())
                    bw.WriteAlignment(0x40);

                var entry = new EncFileEntry
                {
                    offset = fileOffset,
                    size = (uint)writtenSize,
                    decompSize = (int)file.FileSize
                };
                if (file.UsesCompression)
                    entry.size |= 0x80000000;

                entries.Add(entry);
            }

            // Write entries
            bw.BaseStream.Position = 0;

            bw.Write(files.Count);
            bw.WriteMultiple(entries);
        }

        private ArchiveFileInfo CreateAfi(Stream file, string name, EncFileEntry entry)
        {
            if (entry.IsCompressed)
                return new ArchiveFileInfo(file, name, Kompression.Implementations.Compressions.LzEnc, entry.decompSize);

            return new ArchiveFileInfo(file, name);
        }
    }
}
