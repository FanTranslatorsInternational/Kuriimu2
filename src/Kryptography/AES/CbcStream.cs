using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Kryptography.AES
{
    public class CbcStream : KryptoStream
    {
        private Stream _stream;
        private long _length;
        private Aes _aes;

        public override int BlockSize => 128;
        public override int BlockSizeBytes => 16;
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override byte[] IV { get; }
        public override List<byte[]> Keys { get; }
        public override int KeySize => Keys?[0]?.Length ?? 0;
        public override long Length => _length;
        public override long Position { get => _stream.Position; set => Seek(value, SeekOrigin.Begin); }
        private long TotalBlocks => GetBlockCount(Length);

        private long GetBlockCount(long input) => (long)Math.Ceiling((double)input / BlockSizeBytes);
        private long GetCurrentBlock(long input) => input / BlockSizeBytes;

        public CbcStream(byte[] input, byte[] key, byte[] iv) : this(new MemoryStream(input), key, iv) { }

        public CbcStream(Stream input, byte[] key, byte[] iv)
        {
            _stream = input;
            _length = input.Length;

            Keys = new List<byte[]>();
            Keys.Add(key);
            IV = iv;

            _aes = Aes.Create();
            _aes.Padding = PaddingMode.None;
            _aes.Mode = CipherMode.CBC;
        }

        new public void Dispose()
        {
            _stream.Dispose();
            _aes.Dispose();
        }

        public override void Flush()
        {
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
            var originalPosition = Position;

            var blockPosition = Position / BlockSizeBytes * BlockSizeBytes;
            var bytesIntoBlock = Position % BlockSizeBytes;

            count = (int)Math.Min(count, Length - Position);
            var alignedCount = (int)GetBlockCount(bytesIntoBlock + count) * BlockSizeBytes;

            if (alignedCount == 0) return alignedCount;

            var stateVector = PeekLastValidVector();
            var decData = Decrypt(blockPosition, alignedCount, stateVector);

            Array.Copy(decData, bytesIntoBlock, buffer, offset, count);

            Seek(originalPosition + count, SeekOrigin.Begin);

            return count;
        }

        private byte[] Decrypt(long begin, int alignedCount, byte[] iv)
        {
            _stream.Position = begin;
            var readData = new byte[alignedCount];
            _stream.Read(readData, 0, alignedCount);

            var decData = new byte[alignedCount];
            _aes.CreateDecryptor(Keys[0], iv).TransformBlock(readData, 0, readData.Length, decData, 0);

            return decData;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException("Seek is not supported.");

            return _stream.Seek(offset, origin);
        }

        private byte[] PeekLastValidVector()
        {
            var ivBuffer = new byte[BlockSizeBytes];
            Array.Copy(IV, ivBuffer, BlockSizeBytes);

            var length = GetBlockCount(Length) * BlockSizeBytes;
            if (length == 0 || Position < BlockSizeBytes)
                return ivBuffer;

            var originalPosition = Position;
            _stream.Position = (GetCurrentBlock(Math.Min(length, Position)) - 1) * BlockSizeBytes;

            _stream.Read(ivBuffer, 0, ivBuffer.Length);

            _stream.Position = originalPosition;

            return ivBuffer;
        }

        private long GetBlocksBetween(long position)
        {
            var offsetBlock = GetCurrentBlock(position);
            var lengthBlock = GetCurrentBlock(Length);
            if (Math.Max(offsetBlock, lengthBlock) - Math.Min(offsetBlock, lengthBlock) > 1)
                return Math.Max(offsetBlock, lengthBlock) - Math.Min(offsetBlock, lengthBlock) - 1;
            else
                return 0;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateWrite(buffer, offset, count);

            if (count == 0) return;
            var readBuffer = GetInitializedReadBuffer(count, out var dataStart);

            var originalPosition = Position;
            Position -= dataStart;

            var stateVector = PeekLastValidVector();

            PeekOverlappingData(readBuffer, 0, count, stateVector);

            Array.Copy(buffer, offset, readBuffer, dataStart, count);

            var encBuffer = new byte[readBuffer.Length];
            _aes.CreateEncryptor(Keys[0], stateVector).TransformBlock(readBuffer, 0, readBuffer.Length, encBuffer, 0);

            _stream.Write(encBuffer, 0, encBuffer.Length);

            if (originalPosition + count > _length)
                _length = originalPosition + count;

            Seek(originalPosition + count, SeekOrigin.Begin);
        }

        private void ValidateWrite(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Write is not supported");
            if (Position < Length)
                throw new NotSupportedException("Rewriting data is not supported.");
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

        private void PeekOverlappingData(byte[] buffer, int offset, int count, byte[] stateVector)
        {
            if (Position - offset < Length)
            {
                long originalPosition = Position;

                var readBuffer = Decrypt(Position - offset, (int)GetBlockCount(Math.Min(Length - (Position - offset), count)) * BlockSizeBytes, stateVector);
                Array.Copy(readBuffer, 0, buffer, 0, readBuffer.Length);

                Position = originalPosition;
            }
        }
    }
}