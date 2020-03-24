using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_koei_tecmo.Archives
{
    // TODO: Test plugin
    // TODO: Add save
    // Game: Yo-Kai Watch: Sangoukushi
    class X3
    {
        private X3Header _header;

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<X3Header>();

            // Read file entries
            var entries = br.ReadMultiple<X3FileEntry>(_header.fileCount);

            // Add files
            var result = new List<ArchiveFileInfo>();
            foreach (var entry in entries)
            {
                br.BaseStream.Position = entry.offset * _header.alignment + (entry.IsCompressed ? 0x4 : 0);

                Stream firstBlock;
                if (entry.IsCompressed)
                {
                    var firstBlockLength = br.ReadInt32();
                    firstBlock = PeekFirstCompressedBlock(input, input.Position, firstBlockLength);
                }
                else
                    firstBlock = new SubStream(input, br.BaseStream.Position, 4);

                using var fileBr = new BinaryReaderX(firstBlock);
                var magic = fileBr.ReadCStringASCII();
                var extension = ".bin";

                if (magic == "GT1G")
                    extension = ".3ds.g1t";
                else if (magic == "SMDH")
                    extension = ".icn";

                var fileStream = new SubStream(br.BaseStream, entry.offset * _header.alignment, entry.compressedSize);
                var fileName = result.Count.ToString("00000000") + extension;
                if (entry.IsCompressed)
                    result.Add(new ArchiveFileInfo(fileStream, fileName,
                        Kompression.Implementations.Compressions.ZLib, entry.decompressedSize));
                else
                    result.Add(new ArchiveFileInfo(fileStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {

        }

        private Stream PeekFirstCompressedBlock(Stream input, long offset, long firstBlockSize)
        {
            var subStream = new SubStream(input, offset, firstBlockSize);
            var ms = new MemoryStream();

            Kompression.Implementations.Compressions.ZLib.Build().Decompress(subStream, ms);

            ms.Position = 0;
            return ms;
        }
    }
}
