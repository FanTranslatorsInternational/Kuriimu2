using System;
using System.IO;
using System.Security.Cryptography;

namespace Kryptography.AES
{
    public class CbcStream : Stream
    {
        private readonly Aes _aes;
        private readonly byte[] _key;
        private readonly byte[] _initialIv;
        private readonly byte[] _currentIv;

        private readonly Stream _baseStream;
        private long _internalLength;
        private readonly byte[] _lastBlockBuffer;

        private static int BlockSize => 16;

        public override bool CanRead => _baseStream.CanRead && true;

        public override bool CanSeek => _baseStream.CanSeek && true;

        public override bool CanWrite => _baseStream.CanWrite && true;

        public override long Length => _internalLength;

        public override long Position { get; set; }

        public CbcStream(Stream input, byte[] key, byte[] iv)
        {
            if (key.Length / 4 < 4 || key.Length / 4 > 8 || key.Length % 4 > 0)
                throw new InvalidOperationException("Key has invalid length.");

            if (iv.Length != BlockSize)
                throw new InvalidOperationException($"IV needs a length of {BlockSize}.");

            if (input.Length % BlockSize != 0)
                throw new InvalidOperationException($"Stream needs to have a length dividable by {BlockSize}");

            _baseStream = input;

            _key = new byte[key.Length];
            Array.Copy(key, _key, key.Length);
            _initialIv = new byte[iv.Length];
            Array.Copy(iv, _initialIv, iv.Length);
            _currentIv = new byte[iv.Length];
            Array.Copy(iv, _currentIv, iv.Length);

            _internalLength = input.Length;
            _lastBlockBuffer = new byte[BlockSize];

            _aes = Aes.Create() ?? throw new ArgumentNullException(nameof(_aes));
            _aes.Padding = PaddingMode.None;
            _aes.Mode = CipherMode.CBC;
        }

        private void SetIvByPosition(long pos)
        {
            var alignedPos = pos / BlockSize * BlockSize;

            if (alignedPos < BlockSize)
            {
                Array.Copy(_initialIv, _currentIv, BlockSize);
            }
            else
            {
                long bkPos = _baseStream.Position;
                _baseStream.Position = alignedPos - BlockSize;
                _baseStream.Read(_currentIv, 0, BlockSize);
                _baseStream.Position = bkPos;
            }
        }

        public override void Flush()
        {
            if (_internalLength % BlockSize <= 0)
                return;

            var thisBkPos = Position;
            Position = Length / BlockSize * BlockSize;

            var bkPos = _baseStream.Position;
            _baseStream.Position = Position;
            _baseStream.Write(_lastBlockBuffer, 0, BlockSize);
            _baseStream.Position = bkPos;

            _internalLength = _baseStream.Length;
            Position = thisBkPos;

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
                    _baseStream.Read(_lastBlockBuffer,0,0x10);
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

            SetIvByPosition(alignedPosition);

            var internalBuffer = new byte[alignedCount];
            if (alignedPosition + alignedCount > Length)
                Array.Copy(_lastBlockBuffer, 0, internalBuffer, alignedCount - BlockSize, BlockSize);

            var bkPos = _baseStream.Position;
            _baseStream.Position = alignedPosition;
            _baseStream.Read(internalBuffer, 0, (int)alignedCount - (alignedPosition + alignedCount > Length ? BlockSize : 0));
            _baseStream.Position = bkPos;

            _aes.CreateDecryptor(_key, _currentIv).TransformBlock(internalBuffer, 0, (int)alignedCount, internalBuffer, 0);

            Array.Copy(internalBuffer, diffPos, buffer, offset, count);
            Position += count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            if (Position < Length)
                throw new InvalidOperationException($"Can't rewrite data in {nameof(CbcStream)}.");

            var alignedReadStart = Length / BlockSize * BlockSize;
            var alignedCount = RoundUpToMultiple(Position + count, BlockSize) - alignedReadStart;
            var readLength = Length % BlockSize;

            SetIvByPosition(alignedReadStart);

            var internalBuffer = new byte[alignedCount];
            if (readLength > 0)
                _aes.CreateDecryptor(_key, _currentIv).TransformBlock(_lastBlockBuffer, 0, BlockSize, internalBuffer, 0);

            Array.Copy(buffer, offset, internalBuffer, Position - alignedReadStart, count);
            _aes.CreateEncryptor(_key, _currentIv).TransformBlock(internalBuffer, 0, (int)alignedCount, internalBuffer, 0);

            var bkPos = _baseStream.Position;
            _baseStream.Position = alignedReadStart;
            _baseStream.Write(internalBuffer, 0, (int)alignedCount);
            _baseStream.Position = bkPos;

            Array.Copy(internalBuffer, alignedCount - BlockSize, _lastBlockBuffer, 0, BlockSize);

            Position += count;
            _internalLength = Position;
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