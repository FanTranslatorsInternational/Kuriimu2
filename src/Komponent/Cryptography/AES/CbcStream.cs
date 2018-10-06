using Kontract.Abstracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Komponent.Cryptography.AES
{
    public class CbcStream : KryptoStream
    {
        private Stream _stream;
        private long _length;
        private Aes _aes;
        private byte[] _lastValidIV;
        private long _blocksBetweenLengthPosition = 0;
        private long _blockPosition = 0;
        private long _bytesIntoBlock = 0;

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
            _lastValidIV = iv;

            _aes = Aes.Create();
            _aes.Padding = PaddingMode.None;
            _aes.Mode = CipherMode.CBC;
        }

        public override void Flush()
        {
            //if (Position % BlockSizeBytes > 0)
            //    Position -= Position % BlockSizeBytes;
            //_stream.Write(_finalBlock, 0, _finalBlock.Length);
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

            count = (int)Math.Min(count, Length - Position);
            var alignedCount = (int)GetBlockCount(_bytesIntoBlock + count) * BlockSizeBytes;

            if (alignedCount == 0) return alignedCount;

            var decData = Decrypt(_blockPosition, alignedCount, _lastValidIV);

            Array.Copy(decData, _bytesIntoBlock, buffer, offset, count);

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

            UpdateSeekable(offset, origin);
            return _stream.Seek(offset, origin);
        }

        private void UpdateSeekable(long offset, SeekOrigin origin)
        {
            var newOffset = 0L;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    newOffset = offset;
                    break;
                case SeekOrigin.Current:
                    newOffset = Position + offset;
                    break;
                case SeekOrigin.End:
                    newOffset = Length + offset;
                    break;
            }

            _lastValidIV = PeakLastValidIV(newOffset);
            _blocksBetweenLengthPosition = GetBlocksBetween(newOffset);
            _blockPosition = newOffset / BlockSizeBytes * BlockSizeBytes;
            _bytesIntoBlock = newOffset % BlockSizeBytes;
        }

        private byte[] PeakLastValidIV(long position)
        {
            var length = GetBlockCount(Length) * BlockSizeBytes;
            if (length == 0 || position < BlockSizeBytes)
                return IV;

            var originalPosition = Position;
            _stream.Position = (GetCurrentBlock(Math.Min(length, position)) - 1) * BlockSizeBytes;

            var ivBuffer = new byte[BlockSizeBytes];
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

            PeakOverlappingData(readBuffer, (int)dataStart, count);

            Array.Copy(buffer, offset, readBuffer, dataStart, count);

            var encBuffer = new byte[readBuffer.Length];
            _aes.CreateEncryptor(Keys[0], _lastValidIV).TransformBlock(readBuffer, 0, readBuffer.Length, encBuffer, 0);

            var originalPosition = Position;
            _stream.Position -= dataStart;
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
            dataStart = _bytesIntoBlock;

            var bufferBlocks = GetBlockCount(_bytesIntoBlock + count);
            if (Position >= Length)
            {
                bufferBlocks += _blocksBetweenLengthPosition;
                dataStart += _blocksBetweenLengthPosition * BlockSizeBytes;
            }

            var bufferLength = bufferBlocks * BlockSizeBytes;

            return new byte[bufferLength];
        }

        private void PeakOverlappingData(byte[] buffer, int offset, int count)
        {
            if (Position - offset < Length)
            {
                long originalPosition = Position;
                var readBuffer = Decrypt(Position - offset, (int)GetBlockCount(Math.Min(Length - (Position - offset), count)) * BlockSizeBytes, _lastValidIV);
                Array.Copy(readBuffer, 0, buffer, 0, readBuffer.Length);
                Position = originalPosition;
            }
        }
    }
}