using System;
using System.Collections.Generic;
using System.IO;

namespace Kryptography
{
    public abstract class KryptoStream : Stream
    {
        private Stream _baseStream;

        public abstract int BlockSize { get; }
        public abstract int BlockSizeBytes { get; }

        protected abstract int BlockAlign { get; }

        public abstract List<byte[]> Keys { get; protected set; }
        public abstract int KeySize { get; }

        public abstract byte[] IV { get; protected set; }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;

        public KryptoStream(Stream input)
        {
            _baseStream = input;
        }

        public KryptoStream(Stream input, long offset, long length)
        {
            _baseStream = new SubStream(input, offset, length);
        }

        public KryptoStream(byte[] input)
        {
            _baseStream = new MemoryStream(input);
        }

        public KryptoStream(byte[] input, long offset, long length)
        {
            _baseStream = new SubStream(input, offset, length);
        }

        protected abstract int ProcessRead(long alignedPosition, int alignedCount, byte[] decData, int offset);

        protected abstract int ProcessWrite(byte[] buffer, int offset, int count, long alignedPosition);

        #region Overrides
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateRead();
            ValidateInput(buffer, offset, count);

            if (_fixedLength && Position >= Length)
                return 0;

            var alignPos = Position / BlockAlign * BlockAlign;
            var bytesIn = (int)(Position % BlockAlign);
            var alignedCount = GetAlignedCount(count, bytesIn);
            if (alignedCount == 0) return alignedCount;

            var originalPosition = Position;

            var decData = new byte[alignedCount];
            ProcessKryptoRead(alignPos, alignedCount, decData, 0);

            Array.Copy(decData, bytesIn, buffer, offset, count);

            Seek(originalPosition + count, SeekOrigin.Begin);

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateWrite();
            ValidateInput(buffer, offset, count);
            if (count == 0) return;

            var readBuffer = GetInitializedReadBuffer(count, out var dataStart);

            PeakOverlappingData(readBuffer, (int)dataStart, count);

            Array.Copy(buffer, offset, readBuffer, dataStart, count);

            var originalPosition = Position;
            ProcessKryptoWrite(readBuffer, 0, readBuffer.Length, _stream.Position - dataStart);

            if (originalPosition + count > _length)
                _length = originalPosition + count;

            Seek(originalPosition + count, SeekOrigin.Begin);
        }

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
        #endregion

        #region Private Methods
        private int GetAlignedCount(int origCount, int bytesIn)
        {
            var minCount = (double)Math.Min(origCount, Length - Position);
            return (int)Math.Ceiling(minCount / BlockAlign) * BlockAlign;
        }

        private void ValidateRead()
        {
            if (!CanRead)
                throw new NotSupportedException("Reading is not supported.");
        }

        private void ValidateWrite()
        {
            if (!CanWrite)
                throw new NotSupportedException("Write is not supported");
        }

        private void ValidateInput(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException("Offset or count can't be negative.");
            if (offset + count > buffer.Length)
                throw new InvalidDataException("Buffer too short.");
        }

        private byte[] GetInitializedReadBuffer(int count, out long dataStart)
        {
            var blocksBetweenLengthPosition = GetBlocksBetween(Position);
            var bytesIntoBlock = Position % BlockAlign;

            dataStart = bytesIntoBlock;

            var bufferBlocks = GetBlockCount(bytesIntoBlock + count);
            if (Position >= Length)
            {
                bufferBlocks += blocksBetweenLengthPosition;
                dataStart += blocksBetweenLengthPosition * BlockAlign;
            }

            var bufferLength = bufferBlocks * BlockAlign;

            return new byte[bufferLength];
        }

        private long GetBlocksBetween(long position)
        {
            var offsetBlock = GetCurrentBlock(position);
            var lengthBlock = GetCurrentBlock(Length);
            if (Math.Max(offsetBlock, lengthBlock) - Math.Min(offsetBlock, lengthBlock) > 1)
            {
                var res = Math.Max(offsetBlock, lengthBlock) - Math.Min(offsetBlock, lengthBlock);
                if (Length % BlockAlign != 0)
                    res -= 1;
                return res;
            }
            else
                return 0;
        }

        private long GetCurrentBlock(long input) => input / BlockAlign;
        private long GetBlockCount(long input) => (long)Math.Ceiling((double)input / BlockAlign);

        private void PeakOverlappingData(byte[] buffer, int offset, int count)
        {
            if (Position - offset < Length)
            {
                long originalPosition = Position;

                var readBuffer = new byte[(int)GetBlockCount(Math.Min(Length - (Position - offset), count)) * BlockAlign];
                ProcessKryptoRead(Position - offset, (int)GetBlockCount(Math.Min(Length - (Position - offset), count)) * BlockAlign, readBuffer, 0);

                Array.Copy(readBuffer, 0, buffer, 0, readBuffer.Length);

                Position = originalPosition;
            }
        }
        #endregion

        public new void Dispose()
        {
            _baseStream.Dispose();
        }
    }
}