using System.Buffers.Binary;
using Komponent.Contract.Aspects;
using Komponent.Streams;
using Kompression;
using Kompression.Contract;

namespace plugin_sony.Archives.PSARC
{
    public class PsarcHeader
    {
        public string Magic;
        public ushort Major;
        public ushort Minor;
        public string Compression;
        public int TocSize; // zSize
        public int TocEntrySize;
        public int TocEntryCount;
        public int BlockSize;
        public ArchiveFlags ArchiveFlags;

        public string Version => $"v{Major}.{Minor}";
    }

    public sealed class PsarcEntry
    {
        public byte[] MD5Hash;
        public int FirstBlockIndex;
        public PsarcSizeInfo SizeInfo;
    }

    [BitFieldInfo(BlockSize = 1)]
    public class PsarcSizeInfo
    {
        [BitField(40)]
        public long UncompressedSize; // 40 bit (5 bytes)
        [BitField(40)]
        public long Offset; // 40 bit (5 bytes)
    }

    public enum ArchiveFlags
    {
        RelativePaths = 0,
        IgnoreCasePaths = 1,
        AbsolutePaths = 2
    }

    class PsarcStream : Stream
    {
        private static readonly ICompression ZLib = Compressions.ZLib.Build();

        private readonly Stream _baseStream;
        private readonly int _decompBlockSize;
        private readonly long _decompSize;
        private readonly IList<(int, int)> _blocks;
        private readonly Stream[] _decompBlocks;
        private readonly byte[] _blockBuffer;

        private long _position;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _decompSize;
        public override long Position { get => _position; set => Seek(value, SeekOrigin.Begin); }

        public PsarcStream(Stream baseStream, int decompBlockSize, PsarcEntry entry, IList<(int, int)> blockInfos)
        {
            _baseStream = baseStream;
            _decompBlockSize = decompBlockSize;
            _decompSize = entry.SizeInfo.UncompressedSize;

            var blockCount = (int)Math.Ceiling((double)entry.SizeInfo.UncompressedSize / decompBlockSize);
            _blocks = blockInfos.Skip(entry.FirstBlockIndex).Take(blockCount).ToArray();
            _decompBlocks = new Stream[blockCount];

            _blockBuffer = new byte[decompBlockSize];
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
            using var compBlockStream = new SubStream(_baseStream, _blocks[blockIndex].Item1, _blocks[blockIndex].Item2);

            // Decompress the block
            _baseStream.Position = _blocks[blockIndex].Item1;

            var buffer = new byte[2];
            _baseStream.Read(buffer);
            _baseStream.Position -= 2;

            switch (BinaryPrimitives.ReadUInt16BigEndian(buffer))
            {
                case PSARC.ZLibHeader:
                    ZLib.Decompress(_baseStream, _decompBlocks[blockIndex]);
                    break;

                default:
                    CopyBlock(_baseStream, _decompBlocks[blockIndex], _blocks[blockIndex].Item2);
                    break;
            }
        }

        private void CopyBlock(Stream input, Stream output, int blockSize)
        {
            input.Read(_blockBuffer, 0, blockSize);
            output.Write(_blockBuffer);
        }
    }
}
