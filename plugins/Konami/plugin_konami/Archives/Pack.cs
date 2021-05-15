using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

// NOTE: Each entry has information about decompressed and compressed size and offset per file.
// NOTE: Each entry contains a flag marking a file to be compressed or not
// NOTE: The pack first stores all uncompressed and then all compressed files
// NOTE: For uncompressed files, compressed size and offset are either 0 or equal to decompressed size and offset (equal for TEX files, otherwise 0)
// NOTE: SERI files reference this packs string table, which makes them directly dependent to it

// TODO: Maybe create an extra state and form to better incorporate this pack format and its quirks?

namespace plugin_konami.Archives
{
    class Pack
    {
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<PackHeader>();

            // Read entries
            var entries = br.ReadMultiple<PackEntry>(header.fileCount);

            // Read string offsets
            input.Position = header.stringOffsetsOffset;
            var stringOffsets = br.ReadMultiple<int>(header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < header.fileCount; i++)
            {
                var entry = entries[i];
                var stringOffset = stringOffsets[i];

                var offset = entry.IsCompressed ? entry.compOffset : entry.decompOffset;
                var size = entry.IsCompressed ? entry.compSize : entry.decompSize;
                var subStream = new SubStream(input, offset, size);

                input.Position = header.stringOffset + stringOffset;
                var fileName = br.ReadCStringASCII();

                // It seems that for TEX files, the names are stored, but not referenced at all
                // Instead for TEX, the TEXI names are referenced and will be shortened to .tex
                if (entry.magic == "TEX ")
                    fileName = fileName[..^1];

                result.Add(CreateAfi(subStream, fileName, entry));
            }

            return result;
        }

        private IArchiveFileInfo CreateAfi(Stream file, string fileName, PackEntry entry)
        {
            if (entry.IsCompressed)
                return new ArchiveFileInfo(file, fileName, Kompression.Implementations.Compressions.ZLib, entry.decompSize);

            return new ArchiveFileInfo(file, fileName);
        }
    }
}
