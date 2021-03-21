using System;
using System.IO;
using Komponent.IO.Attributes;

namespace plugin_mt_framework.Archives
{
    [Alignment(0x10)]
    class HfsHeader
    {
        [FixedLength(4)]
        public string magic;
        public short version;
        public short type;
        public int fileSize;
    }

    class HfsStream : Stream
    {
        private readonly Stream _baseStream;

        public const int BlockSize = 0x20000;
        public const int VerificationSize = 0x10;
        public const int DataBlockSize = BlockSize - VerificationSize;

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => GetLength();
        public override long Position { get; set; }

        public HfsStream(Stream baseStream)
        {
            _baseStream = baseStream;
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;

                case SeekOrigin.Current:
                    Position += offset;
                    break;

                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }

            return Position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new InvalidOperationException("Read is not supported by the stream.");

            // Cap count
            count = (int)Math.Min(count, Length - Position);

            // Read blocks until count <= 0
            var totalRead = 0;
            while (count > 0)
            {
                var read = ReadBlock(buffer, offset, count);
                totalRead += read;

                // security break, if the last read didn't yield new data, but count is not <= 0
                if (read == 0)
                    break;

                offset += read;
                count -= read;
            }

            return totalRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Reads a full block of data with its verification block and skips the verification block.
        /// Advances the <see cref="Position"/>.
        /// </summary>
        /// <param name="count">The amount of data to read. Will be capped at <see cref="BlockSize"/>.</param>
        /// <returns>The amount of data read for this block. Does not include the verification block size.</returns>
        private int ReadBlock(byte[] buffer, int offset, int count)
        {
            // Calculate amount of data to read in current block
            var blockPosition = Position % DataBlockSize;
            count = (int)Math.Min(count, DataBlockSize - blockPosition);
            count = (int)Math.Min(count, Length - Position);

            // Prepare position in base stream
            _baseStream.Position = Position / DataBlockSize * BlockSize + blockPosition;

            // Read amount of data to buffer
            _baseStream.Read(buffer, offset, count);

            // Advance position
            Position += count;

            return count;
        }

        /// <summary>
        /// Gets the length of the stream without verification blocks.
        /// </summary>
        /// <returns>The length of the stream.</returns>
        private long GetLength()
        {
            var length = _baseStream.Length / BlockSize * DataBlockSize;
            if (_baseStream.Length % BlockSize > VerificationSize)
                length += _baseStream.Length % BlockSize - VerificationSize;

            return length;
        }
    }
}
