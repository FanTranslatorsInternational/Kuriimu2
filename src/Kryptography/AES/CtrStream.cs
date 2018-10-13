using Kryptography.AES.CTR;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kryptography.AES
{
    public class CtrStream : KryptoStream
    {
        private Stream _stream;
        private CtrCryptoTransform _decryptor;
        private CtrCryptoTransform _encryptor;

        private long _offset;
        private long _length;
        private bool _fixedLength = false;
        private bool _littleEndianCtr;

        public override int BlockSize => 128;
        public override int BlockSizeBytes => 16;
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override byte[] IV { get; }
        public override List<byte[]> Keys { get; }
        public override int KeySize => Keys?[0]?.Length ?? 0;
        public override long Length => _length;
        public override long Position { get => _stream.Position - _offset; set => Seek(value, SeekOrigin.Begin); }
        private long TotalBlocks => GetBlockCount(Length);

        private long GetBlockCount(long input) => (long)Math.Ceiling((double)input / BlockSizeBytes);

        private long GetCurrentBlock(long input) => input / BlockSizeBytes;

        public CtrStream(byte[] input, long offset, long length, byte[] key, byte[] ctr, bool littleEndianCtr = false) : this(new MemoryStream(input), offset, length, key, ctr, littleEndianCtr)
        {
        }

        public CtrStream(Stream input, long offset, long length, byte[] key, byte[] ctr, bool littleEndianCtr = false)
        {
            _stream = input;
            _offset = offset;
            _length = length;
            _fixedLength = true;
            _littleEndianCtr = littleEndianCtr;

            Keys = new List<byte[]>();
            Keys.Add(key);
            IV = ctr;

            var aes = AesCtr.Create();
            _decryptor = (CtrCryptoTransform)aes.CreateDecryptor(key, ctr);
            _encryptor = (CtrCryptoTransform)aes.CreateEncryptor(key, ctr);
        }

        new public void Dispose()
        {
            _stream.Dispose();
            _encryptor.Dispose();
            _decryptor.Dispose();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateRead(buffer, offset, count);

            return ReadDecrypted(buffer, offset, count);
        }

        private void ValidateRead(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Reading is not supported.");
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException("Offset or count can't be negative.");
            if (offset + count > buffer.Length)
                throw new InvalidDataException("Buffer too short.");
        }

        private int ReadDecrypted(byte[] buffer, int offset, int count)
        {
            var blockPosition = Position / BlockSizeBytes * BlockSizeBytes;
            var bytesIntoBlock = Position % BlockSizeBytes;

            var originalPosition = Position;

            count = (int)Math.Min(count, Length - Position);
            var alignedCount = (int)GetBlockCount(bytesIntoBlock + count) * BlockSizeBytes;

            if (alignedCount == 0) return alignedCount;

            var stateVector = CalculateLastValidIV();
            var decData = Decrypt(blockPosition, alignedCount, stateVector);

            Array.Copy(decData, bytesIntoBlock, buffer, offset, count);

            Seek(originalPosition + count, SeekOrigin.Begin);

            return count;
        }

        private byte[] Decrypt(long begin, int alignedCount, byte[] iv)
        {
            _stream.Position = begin + _offset;
            var readData = new byte[alignedCount];
            _stream.Read(readData, 0, alignedCount);

            var decData = new byte[alignedCount];
            _decryptor.IV = iv;
            _decryptor.TransformBlock(readData, 0, readData.Length, decData, 0);

            return decData;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ValidateSeek(offset, origin);

            return _stream.Seek(offset + _offset, origin);
        }

        private void ValidateSeek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException("Seek is not supported.");
            if (_fixedLength)
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        if (_offset + offset >= Length)
                            throw new InvalidDataException("Position can't be set outside set stream length.");
                        break;

                    case SeekOrigin.Current:
                        if (_offset + Position + offset >= Length)
                            throw new InvalidDataException("Position can't be set outside set stream length.");
                        break;

                    case SeekOrigin.End:
                        if (Length + offset >= Length)
                            throw new InvalidDataException("Position can't be set outside set stream length.");
                        break;
                }
        }

        private byte[] CalculateLastValidIV()
        {
            byte[] ivBuffer = new byte[0x10];
            Array.Copy(IV, ivBuffer, BlockSizeBytes);

            var length = GetBlockCount(Length) * BlockSizeBytes;
            if (length < BlockSizeBytes || Position < BlockSizeBytes)
                return ivBuffer;

            var increment = (int)GetCurrentBlock(Math.Min(length, Position));
            IncrementCtr(ivBuffer, increment);

            return ivBuffer;
        }

        private void IncrementCtr(byte[] ctr, int count)
        {
            if (!_littleEndianCtr)
                for (int i = ctr.Length - 1; i >= 0; i--)
                {
                    if (count == 0)
                        break;

                    var check = ctr[i];
                    ctr[i] += (byte)count;
                    count >>= 8;

                    int off = 0;
                    while (i - off - 1 >= 0 && ctr[i - off] < check)
                    {
                        check = ctr[i - off - 1];
                        ctr[i - off - 1]++;
                        off++;
                    }
                }
            else
                for (int i = 0; i < ctr.Length; i++)
                {
                    if (count == 0)
                        break;

                    var check = ctr[i];
                    ctr[i] += (byte)count;
                    count >>= 8;

                    int off = 0;
                    while (i + off + 1 < ctr.Length && ctr[i + off] < check)
                    {
                        check = ctr[i + off + 1];
                        ctr[i + off + 1]++;
                        off++;
                    }
                }
        }

        private long GetBlocksBetween(long position)
        {
            if (Length <= 0)
                return GetCurrentBlock(position);

            var offsetBlock = GetCurrentBlock(position);
            var lengthBlock = GetCurrentBlock(Length);

            if (offsetBlock == lengthBlock)
                return 0;

            var lengthRest = Length % BlockSizeBytes;
            if (lengthRest == 0 && position > Length)
                return Math.Max(offsetBlock, lengthBlock) - Math.Min(offsetBlock, lengthBlock);

            return Math.Max(offsetBlock, lengthBlock) - (Math.Min(offsetBlock, lengthBlock) + 1);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateWrite(buffer, offset, count);

            if (_fixedLength && Position + count >= Length)
                count = (int)(Length - Position);
            if (count == 0) return;
            var readBuffer = GetInitializedReadBuffer(count, out var dataStart);

            var originalPosition = Position;
            Position -= dataStart;

            var stateVector = CalculateLastValidIV();

            PeakOverlappingData(readBuffer, (int)dataStart, count, stateVector);

            Array.Copy(buffer, offset, readBuffer, dataStart, count);

            var encBuffer = new byte[readBuffer.Length];
            _encryptor.IV = stateVector;
            _encryptor.TransformBlock(readBuffer, 0, readBuffer.Length, encBuffer, 0);

            _stream.Write(encBuffer, 0, encBuffer.Length);

            if (originalPosition + count > _length)
                _length = originalPosition + count;

            Seek(originalPosition + count, SeekOrigin.Begin);
        }

        private void ValidateWrite(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Write is not supported");
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException("Offset or count can't be negative.");
            if (offset + count > buffer.Length)
                throw new InvalidDataException("Buffer too short.");
        }

        private byte[] GetInitializedReadBuffer(int count, out long dataStart)
        {
            var blocksBetweenLengthPosition = GetBlocksBetween(Position);
            var bytesIntoBlock = Position % BlockSizeBytes;

            dataStart = bytesIntoBlock;

            var bufferBlocks = GetBlockCount(bytesIntoBlock + count);
            if (Position >= Length)
            {
                bufferBlocks += blocksBetweenLengthPosition;
                dataStart += blocksBetweenLengthPosition * BlockSizeBytes;
            }

            var bufferLength = bufferBlocks * BlockSizeBytes;

            return new byte[bufferLength];
        }

        private void PeakOverlappingData(byte[] buffer, int offset, int count, byte[] stateVector)
        {
            if (Position < Length)
            {
                long originalPosition = Position;
                var readBuffer = Decrypt(Position, (int)GetBlockCount(Math.Min(Length - (Position), count)) * BlockSizeBytes, stateVector);
                Array.Copy(readBuffer, 0, buffer, 0, readBuffer.Length);
                Position = originalPosition;
            }
        }
    }
}