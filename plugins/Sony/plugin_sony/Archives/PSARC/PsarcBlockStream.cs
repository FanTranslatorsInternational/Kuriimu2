using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Komponent.IO;
using Kontract.Models.IO;

namespace plugin_sony.Archives.PSARC
{
    /// <summary>
    /// 
    /// </summary>
    public class PsarcBlockStream : Stream
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly Stream _baseStream;

        /// <summary>
        /// 
        /// </summary>
        private MemoryStream _decompressed = null;

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
        public Entry Entry { get; }

        /// <summary>
        /// 
        /// </summary>
        public int BlockSize { get; }

        /// <summary>
        /// 
        /// </summary>
        public List<int> BlockSizes { get; } = new List<int>();

        #endregion

        /// <summary>
        /// 
        /// </summary>
        private void Decompress()
        {
            if (_decompressed == null)
            {
                _decompressed = new MemoryStream();

                using var br = new BinaryReaderX(_baseStream, true, ByteOrder.BigEndian);
                br.BaseStream.Position = Entry.Offset;

                for (var i = Entry.FirstBlockIndex; i < Entry.FirstBlockIndex + Math.Ceiling((double)Entry.UncompressedSize / BlockSize); i++)
                {
                    if (BlockSizes[i] == 0)
                        _decompressed.Write(br.ReadBytes(BlockSize), 0, BlockSize); // Uncompressed block
                    else
                    {
                        var compression = br.PeekUInt16();

                        var blockStart = br.BaseStream.Position;
                        if (compression == PSARC.ZLibHeader)
                        {
                            br.BaseStream.Position += 2;
                            using (var ds = new DeflateStream(br.BaseStream, CompressionMode.Decompress, true))
                                ds.CopyTo(_decompressed);
                            br.BaseStream.Position = blockStart + BlockSizes[i];
                        }
                        // TODO: Add LZMA decompression support
                        else
                            _decompressed.Write(br.ReadBytes(BlockSizes[i]), 0, BlockSizes[i]);
                    }
                }

                _decompressed.Position = 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseStream"></param>
        /// <param name="entry"></param>
        /// <param name="blockSize"></param>
        /// <param name="blockSizes"></param>
        public PsarcBlockStream(Stream baseStream, Entry entry, int blockSize, List<int> blockSizes)
        {
            _baseStream = baseStream;
            Entry = entry;
            BlockSize = blockSize;
            BlockSizes = blockSizes;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Flush()
        {
            Decompress();
            _decompressed.Flush();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            Decompress();

            return _decompressed.Read(buffer, offset, (int)Math.Min(count, _decompressed.Length - Position)); 
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            Decompress();

            return origin switch
            {
                SeekOrigin.Begin => Position = offset,
                SeekOrigin.Current => Position += offset,
                SeekOrigin.End => Position = _decompressed.Length + offset,
                _ => throw new ArgumentException("Origin is invalid."),
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value) => throw new NotImplementedException();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    }
}
