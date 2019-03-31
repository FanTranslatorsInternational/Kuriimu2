using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Kryptography.AES
{
    public class CbcStream : Stream
    {
        private Aes _aes;
        private byte[] _key;
        private byte[] _initialIv;
        private byte[] _currentIv;

        private Stream _baseStream;
        private long _internalLength;
        private byte[] _lastBlockBuffer;

        private int _blockSize => _key.Length;

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _internalLength;

        public override long Position { get; set; }

        public CbcStream(Stream input, byte[] key, byte[] iv)
        {
            if (key.Length / 4 < 4 || key.Length / 4 > 8 || key.Length % 4 > 0)
                throw new InvalidOperationException("Key has invalid length.");

            if (iv.Length / 4 < 4 || iv.Length / 4 > 8 || iv.Length % 4 > 0)
                throw new InvalidOperationException("IV has invalid length.");

            if (key.Length != iv.Length)
                throw new InvalidOperationException("Key and IV need to be the same length.");

            if (input.Length % key.Length != 0)
                throw new InvalidOperationException("Stream needs to have a length dividable by key length");

            _baseStream = input;

            _key = new byte[key.Length];
            Array.Copy(key, _key, key.Length);
            _initialIv = new byte[iv.Length];
            Array.Copy(iv, _initialIv, iv.Length);
            _currentIv = new byte[iv.Length];
            Array.Copy(iv, _currentIv, iv.Length);

            _internalLength = input.Length;
            _lastBlockBuffer = new byte[key.Length];

            _aes = Aes.Create();
            _aes.Padding = PaddingMode.None;
            _aes.Mode = CipherMode.CBC;
        }

        private void SetIvByPosition(long pos)
        {
            var alignedPos = pos / _blockSize * _blockSize;

            if (alignedPos < _blockSize)
            {
                Array.Copy(_initialIv, _currentIv, _blockSize);
            }
            else
            {
                long bkPos = _baseStream.Position;
                _baseStream.Position = alignedPos - _blockSize;
                _baseStream.Read(_currentIv, 0, _blockSize);
                _baseStream.Position = bkPos;
            }
        }

        public override void Flush()
        {
            if (_internalLength % _blockSize <= 0)
                return;

            Position = Length / _blockSize * _blockSize;

            var bkPos = _baseStream.Position;
            _baseStream.Position = Position;
            _baseStream.Write(_lastBlockBuffer, 0, _blockSize);
            _baseStream.Position = bkPos;

            _internalLength = _baseStream.Length;
            Position += _blockSize;

            Array.Clear(_lastBlockBuffer, 0, _blockSize);

            _baseStream.Flush();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
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

            if (Position + count > Length)
                throw new InvalidOperationException("Can't read beyond stream.");

            InternalRead(buffer, offset, count);

            return count;
        }

        private void InternalRead(byte[] buffer, int offset, int count)
        {
            var alignedPosition = Position / _blockSize * _blockSize;
            var diffPos = Position - alignedPosition;
            var alignedCount = RoundUpToMultiple(Position + count, _blockSize) - alignedPosition;

            SetIvByPosition(alignedPosition);

            var internalBuffer = new byte[alignedCount];
            if (alignedPosition + alignedCount > Length)
                Array.Copy(_lastBlockBuffer, 0, internalBuffer, alignedCount - _blockSize, _blockSize);

            var bkPos = _baseStream.Position;
            _baseStream.Position = alignedPosition;
            _baseStream.Read(internalBuffer, 0, (int)alignedCount - (alignedPosition + alignedCount > Length ? _blockSize : 0));
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

            var posLenDiff = Position - Length;
            var alignedReadStart = Length / _blockSize * _blockSize;
            var alignedCount = RoundUpToMultiple(Position + count, _blockSize) - alignedReadStart;
            var readLength = Length % _blockSize;

            SetIvByPosition(alignedReadStart);

            var internalBuffer = new byte[alignedCount];
            if (readLength > 0)
                _aes.CreateDecryptor(_key, _currentIv).TransformBlock(_lastBlockBuffer, 0, _blockSize, internalBuffer, 0);

            Array.Copy(buffer, offset, internalBuffer, Position - alignedReadStart, count);
            _aes.CreateEncryptor(_key, _currentIv).TransformBlock(internalBuffer, 0, (int)alignedCount, internalBuffer, 0);

            var bkPos = _baseStream.Position;
            _baseStream.Position = alignedReadStart;
            _baseStream.Write(internalBuffer, 0, (int)alignedCount);
            _baseStream.Position = bkPos;

            Array.Copy(internalBuffer, alignedCount - _blockSize, _lastBlockBuffer, 0, _blockSize);

            Position += count;
            _internalLength = Position;
        }

        private long RoundUpToMultiple(long numToRound, int multiple)
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