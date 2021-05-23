using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using Kompression.Implementations;
using Kontract.Kompression;

namespace plugin_sony.Archives.PSARC
{
    public class PsarcBlockStream : Stream
    {
        private static readonly ICompression ZLib = Compressions.ZLib.Build();
        private readonly byte[] _blockBuffer;

        private readonly Stream _baseStream;
        private MemoryStream _decompressed;

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// 
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// 
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// 
        /// </summary>
        public override long Length => _decompressed?.Length ?? 0;

        /// <summary>
        /// 
        /// </summary>
        public override long Position
        {
            get => _decompressed?.Position ?? 0;
            set
            {
                if (_decompressed != null)
                    _decompressed.Position = value;
            }
        }

        // Data
        /// <summary>
        /// 
        /// </summary>
        public PsarcEntry Entry { get; }

        /// <summary>
        /// 
        /// </summary>
        public int BlockSize { get; }

        /// <summary>
        /// 
        /// </summary>
        public List<int> BlockSizes { get; } = new List<int>();

        #endregion

        public PsarcBlockStream(Stream baseStream, PsarcEntry psarcEntry, int blockSize, List<int> blockSizes)
        {
            _baseStream = baseStream;
            Entry = psarcEntry;
            BlockSize = blockSize;
            BlockSizes = blockSizes;

            _blockBuffer = new byte[BlockSize];
        }

        /// <inheritdoc cref="Flush"/>
        public override void Flush()
        {
            Decompress();
            _decompressed.Flush();
        }

        /// <inheritdoc cref="SetLength"/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc cref="Seek"/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            Decompress();

            return origin switch
            {
                SeekOrigin.Begin => Position = offset,
                SeekOrigin.Current => Position += offset,
                SeekOrigin.End => Position = _decompressed.Length + offset,
                _ => throw new ArgumentException($"Invalid origin {origin}.")
            };
        }

        /// <inheritdoc cref="Read"/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            Decompress();

            return _decompressed.Read(buffer, offset, (int)Math.Min(count, _decompressed.Length - Position));
        }

        /// <inheritdoc cref="Write"/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private void Decompress()
        {
            if (_decompressed != null)
                return;

            _decompressed = new MemoryStream();
            var buffer = new byte[2];

            _baseStream.Position = Entry.Offset;
            for (var i = Entry.FirstBlockIndex; i < Entry.FirstBlockIndex + Math.Ceiling((double)Entry.UncompressedSize / BlockSize); i++)
            {
                // Write an uncompressed block
                if (BlockSizes[i] == 0)
                {
                    CopyBlock(_baseStream, _decompressed, BlockSize);
                    continue;
                }

                // Decompress the block
                _baseStream.Read(buffer);
                var compression = BinaryPrimitives.ReadUInt16BigEndian(buffer);

                var blockStart = _baseStream.Position;
                switch (compression)
                {
                    case PSARC.ZLibHeader:
                        ZLib.Decompress(_baseStream, _decompressed);
                        _baseStream.Position = blockStart + BlockSizes[i];
                        break;

                    default:
                        CopyBlock(_baseStream, _decompressed, BlockSizes[i]);
                        break;
                }
            }

            _decompressed.Position = 0;
        }

        private void CopyBlock(Stream input, Stream output, int blockSize)
        {
            input.Read(_blockBuffer, 0, blockSize);
            output.Write(_blockBuffer);
        }
    }
}
