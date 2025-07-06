using System.Buffers.Binary;
using Komponent.Streams;
using Kompression;
using Kompression.Contract;

namespace plugin_koei_tecmo.Archives
{
    class GzHeader
    {
        public int decompBlockSize;
        public int blockCount;
        public int decompSize;
    }

    class GzStream : Stream
    {
        private static readonly ICompression ZLib = Compressions.ZLib.Build();

        private readonly Stream _baseStream;
        private readonly int _decompBlockSize;
        private readonly int _decompSize;
        private readonly IList<(int, int)> _blocks;
        private readonly Stream[] _decompBlocks;

        private long _position;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _decompSize;
        public override long Position { get => _position; set => Seek(value, SeekOrigin.Begin); }

        public GzStream(Stream baseStream, int decompBlockSize, int decompSize, IList<(int, int)> blocks)
        {
            _baseStream = baseStream;
            _decompBlockSize = decompBlockSize;
            _decompSize = decompSize;
            _blocks = blocks;
            _decompBlocks = new Stream[blocks.Count];
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;

                case SeekOrigin.Current:
                    _position += offset;
                    break;

                case SeekOrigin.End:
                    _position = Length + offset;
                    break;
            }

            return _position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var length = count = (int)Math.Min(count, Length - Position);

            var block = (int)(Position / _decompBlockSize);
            var blockPos = Position % _decompBlockSize;

            while (length > 0)
            {
                // Determine block size
                var size = (int)Math.Max(0, Math.Min(_decompBlockSize - blockPos, length));

                // Copy decompressed block content
                EnsureDecompressedBlock(block);
                _decompBlocks[block].Position = blockPos;
                _decompBlocks[block].Read(buffer, offset, size);

                // Update local information
                blockPos = 0;
                block++;

                length -= size;
                offset += size;
                _position += size;
            }

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        private void EnsureDecompressedBlock(int blockIndex)
        {
            if (_decompBlocks[blockIndex] != null)
                return;

            _decompBlocks[blockIndex] = new MemoryStream();
            ZLib.Decompress(new SubStream(_baseStream, _blocks[blockIndex].Item1, _blocks[blockIndex].Item2), _decompBlocks[blockIndex]);
        }

        public static int[] ChunkStream(Stream input, Stream output, int decompBlockSize, int alignment)
        {
            var blockSizes = new int[input.Length / decompBlockSize + (input.Length % decompBlockSize > 0 ? 1 : 0)];
            var buffer = new byte[4];

            var position = 0;
            var blockIndex = 0;
            while (position < input.Length)
            {
                var startPos = output.Position;
                output.Position += 4;

                // Compress block
                var blockStream = new SubStream(input, position, Math.Min(decompBlockSize, input.Length - position));
                ZLib.Compress(blockStream, output);

                var endPos = output.Position;
                output.Position = startPos;

                BinaryPrimitives.WriteInt32LittleEndian(buffer, (int)(endPos - startPos - 4));
                output.Write(buffer);
                blockSizes[blockIndex] = (int)(endPos - startPos);

                output.Position = endPos;
                while (output.Position % alignment > 0)
                    output.Position++;

                position += decompBlockSize;
                blockIndex++;
            }

            return blockSizes;
        }
    }
}
