using Kryptography.AES.XTS;
using System;
using System.Collections.Generic;
using System.IO;
using Kryptography.Extensions;

namespace Kryptography.AES
{
    public class XtsStream : Stream //KryptoStream
    {
        private readonly AesXtsCryptoTransform _decryptor;
        private readonly AesXtsCryptoTransform _encryptor;
        private readonly bool _littleEndianId;
        private readonly bool _advanceSectorId;

        private readonly byte[] _initialId;
        private readonly byte[] _currentId;
        private readonly int _sectorSize;

        private readonly Stream _baseStream;
        private long _internalLength;
        private readonly byte[] _lastBlockBuffer;

        private static int BlockSize => 16;

        public override bool CanRead => _baseStream.CanRead && true;

        public override bool CanSeek => _baseStream.CanSeek && true;

        public override bool CanWrite => _baseStream.CanWrite && true;

        public override long Length => _internalLength;

        public override long Position { get; set; }

        public XtsStream(Stream input, byte[] key, byte[] sectorId, bool advanceSectorId, bool littleEndianId, int sectorSize)
        {
            if (key.Length / 4 < 4 || key.Length / 4 > 8 || key.Length % 4 > 0)
                throw new InvalidOperationException("Key has invalid length.");

            if (sectorId.Length != BlockSize)
                throw new InvalidOperationException("SectorId has invalid length.");

            if (input.Length % BlockSize != 0)
                throw new InvalidOperationException($"Stream needs to have a length dividable by {BlockSize}.");

            if (sectorSize % BlockSize != 0)
                throw new InvalidOperationException($"SectorSize needs to be dividable by {BlockSize}");

            _baseStream = input;
            _internalLength = _baseStream.Length;

            _lastBlockBuffer = new byte[BlockSize];
            _littleEndianId = littleEndianId;
            _advanceSectorId = advanceSectorId;
            _sectorSize = sectorSize;

            _initialId = new byte[sectorId.Length];
            Array.Copy(sectorId, _initialId, sectorId.Length);
            _currentId = new byte[sectorId.Length];
            Array.Copy(sectorId, _currentId, sectorId.Length);

            var xts = AesXts.Create(littleEndianId, sectorSize, advanceSectorId);
            _encryptor = (AesXtsCryptoTransform)xts.CreateEncryptor(key, _initialId);
            _decryptor = (AesXtsCryptoTransform)xts.CreateDecryptor(key, _initialId);
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

        private void SetSectorIdByPosition(long pos)
        {
            if (!_advanceSectorId)
            {
                Array.Copy(_initialId, _currentId, _initialId.Length);
                return;
            }

            var alignedPos = pos / _sectorSize * _sectorSize;

            if (alignedPos < _sectorSize)
            {
                Array.Copy(_initialId, _currentId, BlockSize);
            }
            else
            {
                var toIncrement = new byte[BlockSize];
                Array.Copy(_initialId, toIncrement, BlockSize);
                toIncrement.Increment((int)(alignedPos / _sectorSize), _littleEndianId);
                Array.Copy(toIncrement, _currentId, BlockSize);
            }
        }

        public override void Flush()
        {
            if (_internalLength % BlockSize == 0)
                return;

            var thisBkPos = Position;
            Position = Length / BlockSize * BlockSize;

            var bkPos = _baseStream.Position;
            _baseStream.Position = Position;
            _baseStream.Write(_lastBlockBuffer, 0, _lastBlockBuffer.Length);
            _baseStream.Position = bkPos;

            _internalLength = _baseStream.Length;
            Position = thisBkPos;

            Array.Clear(_lastBlockBuffer, 0, _lastBlockBuffer.Length);

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
            var alignedPosition = Position / _sectorSize * _sectorSize;
            var diffPos = Position - alignedPosition;
            var alignedCount = RoundUpToMultiple(Position + count, BlockSize) - alignedPosition;

            SetSectorIdByPosition(alignedPosition);
            Array.Copy(_currentId, _decryptor.SectorId, BlockSize);

            var internalBuffer = new byte[alignedCount];
            if (alignedPosition + alignedCount > Length)
                Array.Copy(_lastBlockBuffer, 0, internalBuffer, alignedCount - BlockSize, BlockSize);

            var bkPos = _baseStream.Position;
            _baseStream.Position = alignedPosition;
            _baseStream.Read(internalBuffer, 0, (int)alignedCount - (alignedPosition + alignedCount > Length ? BlockSize : 0));
            _baseStream.Position = bkPos;

            var sectorAlignedCount = RoundUpToMultiple(alignedCount, _sectorSize);
            var sectorBuffer = new byte[sectorAlignedCount];
            Array.Copy(internalBuffer, sectorBuffer, alignedCount);
            _decryptor.TransformBlock(sectorBuffer, 0, (int)sectorAlignedCount, sectorBuffer, 0);
            Array.Copy(sectorBuffer, internalBuffer, alignedCount);

            Array.Copy(internalBuffer, diffPos, buffer, offset, count);
            Position += count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            var alignedLengthSector = Length / _sectorSize * _sectorSize;
            var alignedLengthBlock = Length / BlockSize * BlockSize;
            var alignedPos = Position / _sectorSize * _sectorSize;
            var alignedCount = RoundUpToMultiple(Position + count, BlockSize) - Math.Min(alignedPos, alignedLengthSector);

            // Setup buffer
            var internalBuffer = new byte[alignedCount];

            // Read existing data to buffer, if section overlaps with already written data
            var bkPos = _baseStream.Position;
            var lowPos = Math.Min(alignedLengthSector, alignedPos);
            _baseStream.Position = lowPos;
            bool useLastBlockBuffer = Length % BlockSize > 0 && Position + count > alignedLengthBlock;
            long readCount = 0;
            if (Position < Length)
            {
                readCount = (Position + count <= Length) ? alignedCount - (useLastBlockBuffer ? BlockSize : 0) : alignedLengthSector - alignedPos;
                _baseStream.Read(internalBuffer, 0, (int)readCount);
            }
            if (useLastBlockBuffer) Array.Copy(_lastBlockBuffer, 0, internalBuffer, readCount, BlockSize);

            // Set SectorId
            SetSectorIdByPosition(lowPos);
            Array.Copy(_currentId, _decryptor.SectorId, BlockSize);
            Array.Copy(_currentId, _encryptor.SectorId, BlockSize);

            // Decrypt read data
            var decryptCount = readCount + (useLastBlockBuffer ? BlockSize : 0);
            var sectorAlignedDecryptCount = RoundUpToMultiple(decryptCount, _sectorSize);
            var sectorBuffer = new byte[sectorAlignedDecryptCount];
            Array.Copy(internalBuffer, sectorBuffer, decryptCount);
            if (decryptCount > 0) _decryptor.TransformBlock(sectorBuffer, 0, (int)sectorAlignedDecryptCount, sectorBuffer, 0);
            Array.Copy(sectorBuffer, internalBuffer, decryptCount);

            // Copy write data to internal buffer
            Array.Copy(buffer, offset, internalBuffer, Position - lowPos, count);

            // Encrypt buffer
            var sectorAlignedEncryptCount = RoundUpToMultiple(alignedCount, _sectorSize);
            sectorBuffer = new byte[sectorAlignedEncryptCount];
            Array.Copy(internalBuffer, sectorBuffer, alignedCount);
            _encryptor.TransformBlock(sectorBuffer, 0, (int)sectorAlignedEncryptCount, sectorBuffer, 0);
            Array.Copy(sectorBuffer, internalBuffer, alignedCount);

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