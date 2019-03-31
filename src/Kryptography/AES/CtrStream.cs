using Kryptography.AES.CTR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Kryptography.AES
{
    public class CtrStream : Stream
    {
        private CtrCryptoTransform _decryptor;
        private CtrCryptoTransform _encryptor;
        private bool _littleEndianCtr;

        private byte[] _key;
        private byte[] _initialCtr;
        private byte[] _currentCtr;

        private Stream _baseStream;
        private long _internalLength;
        private byte[] _lastBlockBuffer;

        private int _blockSize => _key.Length;

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _internalLength;

        public override long Position { get; set; }

        public CtrStream(Stream input, byte[] key, byte[] ctr, bool littleEndianCtr)
        {
            if ((key.Length / 4 < 4 && key.Length / 4 > 8) || key.Length % 4 > 0)
                throw new InvalidOperationException("Key has invalid length.");

            if ((ctr.Length / 4 < 4 && ctr.Length / 4 > 8) || ctr.Length % 4 > 0)
                throw new InvalidOperationException("Counter has invalid length.");

            if (key.Length != ctr.Length)
                throw new InvalidOperationException("Key and Counter need to be the same length.");

            if (input.Length % key.Length != 0)
                throw new InvalidOperationException("Stream needs to have a length dividable by key length");

            _baseStream = input;

            _key = new byte[key.Length];
            Array.Copy(key, _key, key.Length);
            _initialCtr = new byte[ctr.Length];
            Array.Copy(ctr, _initialCtr, ctr.Length);
            _currentCtr = new byte[ctr.Length];
            Array.Copy(ctr, _currentCtr, ctr.Length);

            _internalLength = input.Length;
            _lastBlockBuffer = new byte[key.Length];
            _littleEndianCtr = littleEndianCtr;

            var aes = AesCtr.Create(littleEndianCtr);
            _decryptor = (CtrCryptoTransform)aes.CreateDecryptor(key, ctr);
            _encryptor = (CtrCryptoTransform)aes.CreateEncryptor(key, ctr);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _encryptor.Dispose();
                _decryptor.Dispose();
            }
        }

        private void SetCtrByPosition(long pos)
        {
            var alignedPos = pos / _blockSize * _blockSize;

            if (alignedPos < _blockSize)
            {
                Array.Copy(_initialCtr, _currentCtr, _blockSize);
            }
            else
            {
                var toIncrement = new byte[_blockSize];
                Array.Copy(_initialCtr, toIncrement, _blockSize);
                toIncrement.Increment((int)(alignedPos / _blockSize), _littleEndianCtr);
                Array.Copy(toIncrement, _currentCtr, _blockSize);
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

            SetCtrByPosition(alignedPosition);
            Array.Copy(_currentCtr, _decryptor.Ctr, _blockSize);

            var internalBuffer = new byte[alignedCount];
            if (alignedPosition + alignedCount > Length)
                Array.Copy(_lastBlockBuffer, 0, internalBuffer, alignedCount - _blockSize, _blockSize);

            var bkPos = _baseStream.Position;
            _baseStream.Position = alignedPosition;
            _baseStream.Read(internalBuffer, 0, (int)alignedCount - (alignedPosition + alignedCount > Length ? _blockSize : 0));
            _baseStream.Position = bkPos;

            _decryptor.TransformBlock(internalBuffer, 0, (int)alignedCount, internalBuffer, 0);

            Array.Copy(internalBuffer, diffPos, buffer, offset, count);
            Position += count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            var alignedLength = Length / _blockSize * _blockSize;
            var alignedPos = Position / _blockSize * _blockSize;
            var alignedCount = RoundUpToMultiple(Position + count, _blockSize) - Math.Min(alignedPos, alignedLength);

            // Setup buffer
            var internalBuffer = new byte[alignedCount];

            // Read existing data to buffer, if section overlaps with already written data
            var bkPos = _baseStream.Position;
            var lowPos = Math.Min(alignedLength, alignedPos);
            _baseStream.Position = lowPos;
            bool useLastBlockBuffer = Length % _blockSize > 0 && Position + count > alignedLength;
            long readCount = 0;
            if (Position < Length)
            {
                readCount = (Position + count <= Length) ? alignedCount - (useLastBlockBuffer ? _blockSize : 0) : alignedLength - alignedPos;
                _baseStream.Read(internalBuffer, 0, (int)readCount);
            }
            if (useLastBlockBuffer) Array.Copy(_lastBlockBuffer, 0, internalBuffer, readCount, _blockSize);

            // Set Ctr
            SetCtrByPosition(lowPos);
            Array.Copy(_currentCtr, _decryptor.Ctr, _blockSize);
            Array.Copy(_currentCtr, _encryptor.Ctr, _blockSize);

            // Decrypt read data
            var decryptCount = readCount + (useLastBlockBuffer ? _blockSize : 0);
            if (decryptCount > 0) _decryptor.TransformBlock(internalBuffer, 0, (int)decryptCount, internalBuffer, 0);

            // Copy write data to internal buffer
            Array.Copy(buffer, offset, internalBuffer, Position - lowPos, count);

            // Encrypt buffer
            _encryptor.TransformBlock(internalBuffer, 0, (int)alignedCount, internalBuffer, 0);

            // Write data to stream
            _baseStream.Position = lowPos;
            if (Position + count > Length) useLastBlockBuffer = (Position + count) % _blockSize > 0;
            _baseStream.Write(internalBuffer, 0, (int)alignedCount - (useLastBlockBuffer ? _blockSize : 0));

            // Fill last buffer, if necessary
            if (useLastBlockBuffer) Array.Copy(internalBuffer, alignedCount - _blockSize, _lastBlockBuffer, 0, _blockSize);

            _baseStream.Position = bkPos;
            if (Position + count > Length) _internalLength = Position + count;
            Position += count;
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