using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_koei_tecmo.Archives
{
    class Gz
    {
        private static readonly int HeaderSize = 0xC;

        private GzHeader _header;
        private IList<int> _blockSizes;
        private Stream _origStream;

        public IArchiveFile Load(Stream input, string fileName = null)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read sizes
            _blockSizes = ReadIntegers(br, _header.blockCount);
            var blockOffsets = new int[_header.blockCount];
            for (var i = 0; i < _header.blockCount; i++)
            {
                input.Position = (input.Position + 0x7F) & ~0x7F;
                blockOffsets[i] = (int)(input.Position + 4);
                input.Position += _blockSizes[i];
            }

            // Create file
            _origStream = new SubStream(input, blockOffsets[0] - 4, input.Length - blockOffsets[0] + 4);
            var fileStream = new GzStream(input, _header.decompBlockSize, _header.decompSize, blockOffsets.Zip(_blockSizes.Select(x => x - 4)).ToArray());
            fileName ??= "00000000.bin";

            return new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = fileName,
                FileData = fileStream
            });
        }

        public void Save(Stream output, IArchiveFile file)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var sizeOffset = HeaderSize;
            var blockCount = file.ContentChanged ? (int)Math.Ceiling(file.FileSize / (float)_header.decompBlockSize) : _blockSizes.Count;
            var dataOffset = (sizeOffset + blockCount * 4 + 0x7F) & ~0x7F;

            // Chunk stream into blocks
            output.Position = dataOffset;
            var blockSizes = _blockSizes;

            if (file.ContentChanged)
            {
                blockSizes = GzStream.ChunkStream(file.GetFileData().Result, output, _header.decompBlockSize, 0x80);
                _header.decompSize = (int)file.FileSize;
            }
            else
            {
                _origStream.Position = 0;
                _origStream.CopyTo(output);
            }

            // Write header
            _header.blockCount = blockSizes.Count;

            output.Position = 0;
            WriteHeader(_header, bw);

            // Write block sizes
            WriteIntegers(blockSizes, bw);
        }

        private GzHeader ReadHeader(BinaryReaderX reader)
        {
            return new GzHeader
            {
                decompBlockSize = reader.ReadInt32(),
                blockCount = reader.ReadInt32(),
                decompSize = reader.ReadInt32()
            };
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WriteHeader(GzHeader header, BinaryWriterX writer)
        {
            writer.Write(header.decompBlockSize);
            writer.Write(header.blockCount);
            writer.Write(header.decompSize);
        }

        private void WriteIntegers(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }
    }
}
