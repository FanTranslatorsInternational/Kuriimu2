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
        protected virtual int BufferSize { get; } = 0x10000;

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

        protected abstract void Decrypt(byte[] buffer, int offset, int count);

        protected abstract void Encrypt(byte[] buffer, int offset, int count);

        #region Overrides
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateRead(buffer, offset, count);
            if (Position >= Length) return 0;

            var alignPos = Position / BlockAlign * BlockAlign;
            var bytesIn = (int)(Position % BlockAlign);
            var alignedCount = GetAlignedCount((int)Math.Min(count, Length - Position));
            if (alignedCount == 0) return 0;

            _baseStream.Position = alignPos;

            var read = 0;
            var decData = new byte[BufferSize];
            while (read < alignedCount)
            {
                var size = Math.Min(alignedCount - read, BufferSize);
                read += _baseStream.Read(decData, 0, size);

                Decrypt(decData, 0, size);

                var copyOffset = (read == size) ? bytesIn : 0;
                var copySize = (read == size) ? size - bytesIn : size;
                copySize -= (read >= alignedCount) ? alignedCount - count : 0;
                Array.Copy(decData, copyOffset, buffer, offset + read, copySize);
            }
            Position += read;

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateWrite(buffer, offset, count);
            if (count == 0) return;

            var alignedPos = Math.Min(Length, Position) / BlockAlign * BlockAlign;
            var bytesIn = (int)(Position % BlockAlign);
            var alignedCount = GetAlignedCount((int)(Position - alignedPos + count));
            if (alignedCount == 0) return;

            _baseStream.Position = alignedPos;

            var preDecData = new byte[Length - alignedPos];
            PreDecryption(preDecData, 0, (int)(Length - alignedPos));

            var write = 0;
            var decData = new byte[BufferSize];
            while (write < alignedCount)
            {
                var preSize = Math.Min(Math.Max(0, (int)(Length - alignedPos - write)), BufferSize);
                var zeroSize = (Position > Length) ? Math.Min(Math.Max(0, Position - Length - Math.Max(0, alignedPos + write - Length)), BufferSize) : 0;
                //TODO
                var encSize = (alignedPos + write + BufferSize >= Position) ? Math.Min(alignedCount - (alignedPos + write + BufferSize - Position), BufferSize) : 0;

                var size = Math.Min(alignedCount - write, BufferSize);
                write += _baseStream.Read(decData, 0, size);

                Decrypt(decData, 0, size);
            }

            var readBuffer = GetInitializedReadBuffer(count, out var dataStart);

            PeakOverlappingData(readBuffer, (int)dataStart, count);

            Array.Copy(buffer, offset, readBuffer, dataStart, count);

            var originalPosition = Position;
            ProcessKryptoWrite(readBuffer, 0, readBuffer.Length, _stream.Position - dataStart);

            if (originalPosition + count > _length)
                _length = originalPosition + count;

            Seek(originalPosition + count, SeekOrigin.Begin);
        }

        private void PreDecryption(byte[] preDecData, int offset, int count)
        {
            if (count <= 0) return;

            _baseStream.Read(preDecData, 0, count);
            Decrypt(preDecData, 0, count);
        }

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
        #endregion

        #region Private Methods
        private void ValidateRead(byte[] buffer, int offset, int count)
        {
            if (!CanRead) throw new NotSupportedException("Reading is not supported.");

            ValidateInput(buffer, offset, count);
        }

        private void ValidateWrite(byte[] buffer, int offset, int count)
        {
            if (!CanWrite) throw new NotSupportedException("Write is not supported");

            ValidateInput(buffer, offset, count);
        }

        private void ValidateInput(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Offset or count can't be negative.");
            if (offset + count > buffer.Length) throw new InvalidDataException("Buffer too short.");
        }

        private int GetAlignedCount(int count)
        {
            return (int)Math.Ceiling((double)count / BlockAlign) * BlockAlign;
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