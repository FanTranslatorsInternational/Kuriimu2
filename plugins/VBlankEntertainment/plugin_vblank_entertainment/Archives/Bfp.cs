using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_vblank_entertainment.Archives
{
    class Bfp
    {
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<BfpHeader>();

            // Read entries
            input.Position = 0x20;
            var entries = br.ReadMultiple<BfpFileEntry>(header.entryCount);

            // Read bucket entries
            var bucketEntries = br.ReadMultiple<BfpBucketFileEntry>(0x100);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < header.entryCount; i++)
            {
                var entry = entries[i];
                input.Position = entry.offset;
                var size = br.ReadInt32();

                var subStream = new SubStream(input, entry.offset + 0x20, size);
                var name = $"{i:00000000}.bin";

                result.Add(CreateAfi(subStream, name, entry.decompSize));
            }

            var count = header.entryCount;
            foreach (var entry in bucketEntries.Where(x => x.offset != 0))
            {
                input.Position = entry.offset;
                var size = br.ReadInt32();

                var subStream = new SubStream(input, entry.offset + 0x20, size);
                var name = $"{count++:00000000}.bin";

                result.Add(CreateAfi(subStream, name, entry.decompSize));
            }

            return result;
        }

        private ArchiveFileInfo CreateAfi(Stream file, string name, int decompSize)
        {
            if (file.Length == decompSize)
                return new ArchiveFileInfo(file, name);

            return new ArchiveFileInfo(file, name, Kompression.Implementations.Compressions.ZLib, decompSize);
        }
    }
}
