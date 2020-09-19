using System;
using System.IO;
using System.Security.Cryptography;

namespace Kryptography.AES
{
    public sealed class EcbStream : Stream
    {
        private readonly ICryptoTransform _decryptor;
        private readonly ICryptoTransform _encryptor;

        private readonly Stream _baseStream;
        private long _internalLength;
        private readonly byte[] _lastBlockBuffer;

        private static int BlockSize => 16;

        public override bool CanRead => _baseStream.CanRead && true;

        public override bool CanSeek => _baseStream.CanSeek && true;

        public override bool CanWrite => _baseStream.CanWrite && true;

        public override long Length => _internalLength;

        public override long Position { get; set; }

        public EcbStream(Stream input, byte[] key)
        {
            if (key.Length / 4 < 4 || key.Length / 4 > 8 || key.Length % 4 > 0)
                throw new InvalidOperationException("Key has invalid length.");

            if (input.Length % BlockSize != 0)
                throw new InvalidOperationException($"Stream needs to have a length dividable by {BlockSize}");

            _baseStream = input;

            _internalLength = input.Length;
            _lastBlockBuffer = new byte[BlockSize];

            Aes aes = Aes.Create() ?? throw new ArgumentNullException(nameof(aes));
            aes.Padding = PaddingMode.None;
            aes.Mode = CipherMode.ECB;

            _decryptor = aes.CreateDecryptor(key, null);
            _encryptor = aes.CreateEncryptor(key, null);
        }

        public override void Flush()
        {
            if (_internalLength % BlockSize <= 0)
                return;

            Position = Length / BlockSize * BlockSize;

            var bkPos = _baseStream.Position;
            _baseStream.Position = Position;
            _baseStream.Write(_lastBlockBuffer, 0, BlockSize);
            _baseStream.Position = bkPos;

            _internalLength = _baseStream.Length;
            Position += BlockSize;

            Array.Clear(_lastBlockBuffer, 0, BlockSize);

            _baseStream.Flush();
        }

        public override void SetLength(long value)
        {
            if (!CanWrite || !CanSeek)
                throw new NotSupportedException("Can't set length of stream.");
            if (value < 0)
                throw new IOException("Length can't be smaller than 0.");

            if (value > Length)
            {
                var bkPosThis = Position;
                var bkPosBase = _baseStream.Position;

                var startPos = Math.Max(_baseStream.Length, Length);
                var newDataLength = value - startPos;
                var written = 0;
                var newData = new byte[0x10000];
                while (written < newDataLength)
                {
                    Position = startPos;
                    var toWrite = (int)Math.Min(0x10000, newDataLength - written);
                    Write(newData, 0, toWrite);
                    written += toWrite;
                    startPos += toWrite;
                }

                _baseStream.Position = bkPosBase;
                Position = bkPosThis;
            }
            else
            {
                if (value % BlockSize != Length % BlockSize)
                {
                    var bkPos = _baseStream.Position;
                    _baseStream.Position = value % BlockSize;
                    _baseStream.Read(_lastBlockBuffer, 0, 0x10);
                    _baseStream.Position = bkPos;
                }
                _baseStream.SetLength(value);
            }

            _internalLength = value;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException("Can't seek stream.");

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
                throw new NotSupportedException("Can't read from stream.");

            var length = (int)Math.Min(Length - Position, count);
            if (length > 0)
                InternalRead(buffer, offset, length);

            return length;
        }

        private void InternalRead(byte[] buffer, int offset, int count)
        {
            var alignedPosition = Position / BlockSize * BlockSize;
            var diffPos = Position - alignedPosition;
            var alignedCount = RoundUpToMultiple(Position + count, BlockSize) - alignedPosition;

            var internalBuffer = new byte[alignedCount];
            if (alignedPosition + alignedCount > Length)
                Array.Copy(_lastBlockBuffer, 0, internalBuffer, alignedCount - BlockSize, BlockSize);

            var bkPos = _baseStream.Position;
            _baseStream.Position = alignedPosition;
            _baseStream.Read(internalBuffer, 0, (int)alignedCount - (alignedPosition + alignedCount > Length ? BlockSize : 0));
            _baseStream.Position = bkPos;

            _decryptor.TransformBlock(internalBuffer, 0, (int)alignedCount, internalBuffer, 0);

            Array.Copy(internalBuffer, diffPos, buffer, offset, count);
            Position += count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            var alignedLength = Length / BlockSize * BlockSize;
            var alignedPos = Position / BlockSize * BlockSize;
            var alignedCount = RoundUpToMultiple(Position + count, BlockSize) - Math.Min(alignedPos, alignedLength);

            // Setup buffer
            var internalBuffer = new byte[alignedCount];

            // Read existing data to buffer, if section overlaps with already written data
            var bkPos = _baseStream.Position;
            var lowPos = Math.Min(alignedLength, alignedPos);
            _baseStream.Position = lowPos;
            bool useLastBlockBuffer = Length % BlockSize > 0 && Position + count > alignedLength;
            long readCount = 0;
            if (Position < Length)
            {
                readCount = (Position + count <= Length) ? alignedCount - (useLastBlockBuffer ? BlockSize : 0) : alignedLength - alignedPos;
                _baseStream.Read(internalBuffer, 0, (int)readCount);
            }
            if (useLastBlockBuffer) Array.Copy(_lastBlockBuffer, 0, internalBuffer, readCount, BlockSize);

            // Decrypt read data
            var decryptCount = readCount + (useLastBlockBuffer ? BlockSize : 0);
            if (decryptCount > 0) _decryptor.TransformBlock(internalBuffer, 0, (int)decryptCount, internalBuffer, 0);

            // Copy write data to internal buffer
            Array.Copy(buffer, offset, internalBuffer, Position - lowPos, count);

            // Encrypt buffer
            _encryptor.TransformBlock(internalBuffer, 0, (int)alignedCount, internalBuffer, 0);

            // Write data to stream
            _baseStream.Position = lowPos;
            if (Position + count > Length) useLastBlockBuffer = (Position + count) % BlockSize > 0;
            _baseStream.Write(internalBuffer, 0, (int)alignedCount - (useLastBlockBuffer ? BlockSize : 0));

            // Fill last buffer, if necessary
            if (useLastBlockBuffer) Array.Copy(internalBuffer, alignedCount - BlockSize, _lastBlockBuffer, 0, BlockSize);

            _baseStream.Position = bkPos;
            if (Position + count > Length) _internalLength = Position + count;
            Position += count;
        }

        private static long RoundUpToMultiple(long numToRound, int multiple)
        {
            if (multiple == 0)
                return numToRound;

            long remainder = numToRound % multiple;
            if (remainder == 0)
                return numToRound;

            return numToRound + multiple - remainder;
        }
    }
}