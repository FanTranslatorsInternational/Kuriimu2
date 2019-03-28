using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Kryptography.AES
{
    public class CbcStream : Stream//KryptoStream
    {
        private Aes _aes;
        private byte[] _key;
        private byte[] _initialIv;
        private byte[] _currentIv;
        private Stream _baseStream;

        private int _blockSize => _key.Length;

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length;

        public override long Position { get => _baseStream.Position; set => Seek(value, SeekOrigin.Begin); }

        public CbcStream(Stream input, byte[] key, byte[] iv)
        {
            if (key.Length / 4 >= 4 && key.Length / 4 <= 8)
                throw new InvalidOperationException("Key has invalid length.");

            if (iv.Length / 4 >= 4 && iv.Length / 4 <= 8)
                throw new InvalidOperationException("IV has invalid length.");

            if (key.Length == iv.Length)
                throw new InvalidOperationException("Key and IV need to be the same length.");

            if (input.Length % key.Length != 0)
                throw new InvalidOperationException("Stream needs to have a length dividable by key length");

            _key = new byte[key.Length];
            Array.Copy(key, _key, key.Length);
            _initialIv = new byte[iv.Length];
            Array.Copy(iv, _initialIv, iv.Length);
            _currentIv = new byte[iv.Length];
            Array.Copy(iv, _currentIv, iv.Length);

            _aes = Aes.Create();
            _aes.Padding = PaddingMode.None;
            _aes.Mode = CipherMode.CBC;
        }

        //protected override void Decrypt(byte[] buffer, int offset, int count)
        //{
        //    var iv = new byte[BlockSizeBytes];
        //    SetCurrentIv(iv);

        //    _aes.CreateDecryptor(Keys[0], iv).TransformBlock(buffer, offset, count, buffer, offset);
        //}

        //protected override void Encrypt(byte[] buffer, int offset, int count)
        //{
        //    var iv = new byte[BlockSizeBytes];
        //    SetCurrentIv(iv);

        //    _aes.CreateEncryptor(Keys[0], iv).TransformBlock(buffer, offset, count, buffer, offset);
        //}

        private void SetIvByPosition()
        {
            long bkPos = _baseStream.Position;
            _baseStream.Position = _baseStream.Position / _blockSize * _blockSize;

            if (_baseStream.Position < _blockSize)
            {
                Array.Copy(_initialIv, _currentIv, _blockSize);
            }
            else
            {
                _baseStream.Position -= _blockSize;
                _baseStream.Read(_currentIv, 0, _blockSize);
            }

            _baseStream.Position = bkPos;
        }

        public override void Flush() => _baseStream.Flush();

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException("Can't seek stream.");

            var newPos = _baseStream.Seek(offset, origin);
            SetIvByPosition();
            return newPos;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Can't read from stream.");

            if (Position + count >= Length)
                throw new InvalidOperationException("Can't read beyond stream.");

            InternalRead(buffer, offset, count);

            return count;
        }

        private void InternalRead(byte[] buffer, int offset, int count)
        {
            var origPos = Position;
            var alignedPosition = Position / _blockSize * _blockSize;
            var diffPos = Position - alignedPosition;
            var alignedCount = RoundUpToMultiple(Position + count, _blockSize) - alignedPosition;

            Position = alignedPosition;

            var internalBuffer = new byte[alignedCount];
            _baseStream.Read(internalBuffer, 0, (int)alignedCount);
            _aes.CreateDecryptor(_key, _currentIv).TransformBlock(internalBuffer, 0, (int)alignedCount, internalBuffer, 0);

            Position = origPos;
            Array.Copy(internalBuffer, diffPos, buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
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