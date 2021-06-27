using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO.Attributes;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Kompression;
using Kontract.Models.IO;

namespace plugin_sony.Archives.PSARC
{
    public class PsarcHeader
    {
        [FixedLength(4)]
        public string Magic;
        public ushort Major;
        public ushort Minor;
        [FixedLength(4)]
        public string Compression;
        public int TocSize; // zSize
        public int TocEntrySize;
        public int TocEntryCount;
        public int BlockSize;
        public ArchiveFlags ArchiveFlags;

        public string Version => $"v{Major}.{Minor}";
    }

    [BitFieldInfo(BlockSize = 1)]
    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    public sealed class PsarcEntry
    {
        [FixedLength(16)]
        public byte[] MD5Hash;
        public int FirstBlockIndex;
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
            _decompSize = entry.UncompressedSize;

            var blockCount = (int)Math.Ceiling((double)entry.UncompressedSize / decompBlockSize);
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

        //public static int[] ChunkStream(Stream input, Stream output, int decompBlockSize, int alignment)
        //{
        //    var blockSizes = new int[input.Length / decompBlockSize + (input.Length % decompBlockSize > 0 ? 1 : 0)];
        //    var buffer = new byte[4];

        //    var position = 0;
        //    var blockIndex = 0;
        //    while (position < input.Length)
        //    {
        //        var startPos = output.Position;
        //        output.Position += 4;

        //        // Compress block
        //        var blockStream = new SubStream(input, position, Math.Min(decompBlockSize, input.Length - position));
        //        ZLib.Compress(blockStream, output);

        //        var endPos = output.Position;
        //        output.Position = startPos;

        //        BinaryPrimitives.WriteInt32LittleEndian(buffer, (int)(endPos - startPos - 4));
        //        output.Write(buffer);
        //        blockSizes[blockIndex] = (int)(endPos - startPos);

        //        output.Position = endPos;
        //        while (output.Position % alignment > 0)
        //            output.Position++;

        //        position += decompBlockSize;
        //        blockIndex++;
        //    }

        //    return blockSizes;
        //}
    }
}
