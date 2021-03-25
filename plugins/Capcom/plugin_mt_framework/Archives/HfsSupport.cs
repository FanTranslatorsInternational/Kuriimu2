using System;
using System.IO;
using Komponent.IO.Attributes;
using Komponent.IO.Streams;
using Kryptography.Hash;

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

    class HfsHash : IHash
    {
        private static readonly byte[] InitValues = { 0x87, 0x55, 0x07, 0xB5, 0x4B, 0x04, 0xA5, 0xAE, 0xC7, 0x67, 0xBE, 0xCB, 0x01, 0x50, 0x58, 0x44 };
        private static readonly int[] RotValues = { 1, 6, 3, 4, 2, 5, 7, 4, 6, 2, 1, 5, 3, 1, 7, 3 };

        public byte[] Compute(Span<byte> input)
        {
            var result = new byte[16];
            Array.Copy(InitValues, result, 16);

            ComputeInternal(input, 0, input.Length, result);

            Finalize(result);

            return result;
        }

        public byte[] Compute(Stream input)
        {
            var result = new byte[16];
            Array.Copy(InitValues, result, 16);

            var buffer = new byte[4096];
            int readSize;
            do
            {
                readSize = input.Read(buffer, 0, 4096);
                ComputeInternal(buffer, 0, readSize, result);
            } while (readSize > 0);

            Finalize(result);

            return result;
        }

        private void ComputeInternal(Span<byte> buffer, int offset, int size, byte[] result)
        {
            for (var i = offset; i < size; i++)
                result[i % 16] += buffer[i];
        }

        private void Finalize(byte[] result)
        {
            for (var i = 0; i < 16; i++)
                result[i] = Rot(result[i], RotValues[i]);
        }

        private static byte Rot(byte value, int rot) => (byte)((value >> rot) | (value << (8 - rot)));
    }

    class HfsStream : Stream
    {
        private readonly Stream _baseStream;

        private static readonly byte[] VerificationPlaceholder = new byte[VerificationSize];
        private static readonly IHash Hash = new HfsHash();

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
            if (!baseStream.CanRead)
                throw new InvalidOperationException("This stream needs to be readable to update the hash blocks.");

            _baseStream = baseStream;
        }

        public override void Flush()
        {
            WriteVerification();

            _baseStream.Flush();
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(GetBaseLength(value));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new InvalidOperationException("Seek is not supported by the stream.");

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
            if (!CanWrite)
                throw new InvalidOperationException("Write is not supported by the stream.");

            // Cap count
            count = Math.Min(count, buffer.Length - offset);

            // Read blocks until count <= 0
            while (count > 0)
            {
                var written = WriteBlock(buffer, offset, count);

                // security break, if the last write didn't yield new data, but count is not <= 0
                if (written == 0)
                    break;

                offset += written;
                count -= written;
            }
        }

        /// <summary>
        /// Reads a full block of data with its verification block and skips the verification block.
        /// Advances the <see cref="Position"/>.
        /// </summary>
        /// <param name="buffer">The buffer to read in.</param>
        /// <param name="offset">The offset into the buffer to read to.</param>
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
        /// Writes a full block of data with a placeholder for the verification block.
        /// </summary>
        /// <param name="buffer">The buffer to write from.</param>
        /// <param name="offset">The offset into the buffer to write from.</param>
        /// <param name="count">The amount of data to write. Will be capped at <see cref="BlockSize"/>.</param>
        /// <returns>The amount of data written for this block. Does not include the verification block size.</returns>
        private int WriteBlock(byte[] buffer, int offset, int count)
        {
            // Calculate amount of data to read in current block
            var blockPosition = Position % DataBlockSize;
            count = (int)Math.Min(count, DataBlockSize - blockPosition);

            // Prepare position in base stream
            _baseStream.Position = Position / DataBlockSize * BlockSize + blockPosition;

            // Write amount of data to base stream
            _baseStream.Write(buffer, offset, count);

            // Skip alignment if alignment is needed
            if (_baseStream.Position % 0x10 > 0)
                _baseStream.Position += 0x10 - _baseStream.Position % 0x10;

            // Write placeholder for verification block, if we are at the end of the stream
            if (_baseStream.Position >= _baseStream.Length)
                _baseStream.Write(VerificationPlaceholder);

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

        /// <summary>
        /// Transforms a given length to account for verification blocks in the <see cref="HfsStream"/>.
        /// </summary>
        /// <param name="length">The length to base the calculation on.</param>
        /// <returns>The length of the base stream.</returns>
        public static long GetBaseLength(long length)
        {
            var result = length / DataBlockSize * BlockSize;
            if (length % DataBlockSize > 0)
            {
                result += length % DataBlockSize;

                // Align to 16 bytes if necessary
                if (result % 0x10 > 0)
                    result += 0x10 - result % 0x10;

                // Add final verification block placeholder
                result += 0x10;
            }

            return result;
        }

        /// <summary>
        /// Writes the verification blocks for the whole stream.
        /// </summary>
        private void WriteVerification()
        {
            var block = new byte[0x1FFF0];
            _baseStream.Position = 0;

            for (var i = 0; i < _baseStream.Length; i += 0x20000)
            {
                var length = (int)Math.Min(DataBlockSize, _baseStream.Length - i - VerificationSize);
                _baseStream.Read(block, 0, length);

                var hash = Hash.Compute(block.AsSpan(0, length));
                _baseStream.Write(hash, 0, hash.Length);
            }
        }
    }
}
