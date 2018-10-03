using Kontract.Abstracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Komponent.Cryptography.AES
{
    public class EcbStream : KryptoStream
    {
        private Stream _stream;
        private ICryptoTransform _decryptor;
        private ICryptoTransform _encryptor;
        private long _blocksBetweenLengthPosition = 0;
        private long _blockPosition = 0;
        private long _bytesIntoBlock = 0;

        public override int BlockSize => 128;
        public override int BlockSizeBytes => 16;
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override byte[] IV => throw new NotImplementedException();
        public override List<byte[]> Keys { get; }
        public override int KeySize => Keys?[0]?.Length ?? 0;
        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => Seek(value, SeekOrigin.Begin); }
        private long TotalBlocks => GetBlockCount(Length);

        private long GetBlockCount(long input) => (long)Math.Ceiling((double)input / BlockSizeBytes);
        private long GetCurrentBlock(long input) => input / BlockSizeBytes;

        public EcbStream(byte[] input, byte[] key) : this(new MemoryStream(input), key) { }

        public EcbStream(Stream input, byte[] key)
        {
            _stream = input;

            Keys = new List<byte[]>();
            Keys.Add(key);

            var aes = Aes.Create();
            aes.Padding = PaddingMode.None;
            aes.Mode = CipherMode.ECB;

            _decryptor = aes.CreateDecryptor(key, null);
            _encryptor = aes.CreateEncryptor(key, null);
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
            count = (int)Math.Min(count, Length - Position);
            var alignedCount = (int)GetBlockCount(_bytesIntoBlock + count) * BlockSizeBytes;

            if (alignedCount == 0) return alignedCount;

            var decData = Decrypt(_blockPosition, alignedCount);

            Array.Copy(decData, _bytesIntoBlock, buffer, offset, count);

            UpdateSeekable(Position, SeekOrigin.Begin);

            return count;
        }

        private byte[] Decrypt(long begin, int alignedCount)
        {
            _stream.Position = begin;
            var readData = new byte[alignedCount];
            _stream.Read(readData, 0, alignedCount);

            var decData = new byte[alignedCount];
            _decryptor.TransformBlock(readData, 0, readData.Length, decData, 0);

            return decData;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            UpdateSeekable(offset, origin);
            return _stream.Seek(offset, origin);
        }

        private void UpdateSeekable(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _blocksBetweenLengthPosition = GetBlocksBetween(offset);
                    _blockPosition = offset / BlockSizeBytes * BlockSizeBytes;
                    _bytesIntoBlock = offset % BlockSizeBytes;
                    break;
                case SeekOrigin.Current:
                    _blocksBetweenLengthPosition = GetBlocksBetween(Position + offset);
                    _blockPosition = (Position + offset) / BlockSizeBytes * BlockSizeBytes;
                    _bytesIntoBlock = (Position + offset) % BlockSizeBytes;
                    break;
                case SeekOrigin.End:
                    _blocksBetweenLengthPosition = GetBlocksBetween(Length + offset);
                    _blockPosition = (Length + offset) / BlockSizeBytes * BlockSizeBytes;
                    _bytesIntoBlock = (Length + offset) % BlockSizeBytes;
                    break;
            }
        }

        private long GetBlocksBetween(long position)
        {
            var offsetBlock = GetCurrentBlock(position);
            var lengthBlock = GetCurrentBlock(Length);
            if (Math.Max(offsetBlock, lengthBlock) - Math.Min(offsetBlock, lengthBlock) > 1)
                return Math.Max(offsetBlock, lengthBlock) - Math.Min(offsetBlock, lengthBlock);
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
            _encryptor.TransformBlock(readBuffer, 0, readBuffer.Length, encBuffer, 0);

            _stream.Position -= dataStart;
            _stream.Write(encBuffer, 0, encBuffer.Length);

            UpdateSeekable(_stream.Position, SeekOrigin.Begin);
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
            if (Position < Length)
            {
                long originalPosition = Position;
                var readBuffer = Decrypt(Position - offset, (int)GetBlockCount(Math.Min(Length - Position, count)) * BlockSizeBytes);
                Array.Copy(readBuffer, 0, buffer, 0, readBuffer.Length);
                Position = originalPosition;
            }
        }

        //private byte[] ReadDecrypted(int count)
        //{
        //    long offsetIntoBlock = 0;
        //    if (Position % BlockSizeBytes > 0)
        //        offsetIntoBlock = Position % BlockSizeBytes;

        //    var blocksToRead = GetBlockCount((int)offsetIntoBlock + count);
        //    var blockPaddedCount = blocksToRead * BlockSizeBytes;

        //    if (Length <= 0 || count <= 0)
        //        return new byte[blockPaddedCount];

        //    var originalPosition = Position;
        //    Position -= offsetIntoBlock;

        //    var minimalDecryptableSize = (int)Math.Min(GetBlockCount(Length) * BlockSizeBytes, blockPaddedCount);
        //    var bytesRead = new byte[minimalDecryptableSize];
        //    _stream.Read(bytesRead, 0, minimalDecryptableSize);
        //    Position = originalPosition;

        //    if (GetBlockCount((int)Position + count) >= TotalBlocks)
        //        Array.Copy(new byte[BlockSizeBytes], 0, bytesRead, bytesRead.Length - BlockSizeBytes, BlockSizeBytes);

        //    var decrypted = _decryptor.TransformFinalBlock(bytesRead, 0, minimalDecryptableSize);
        //    var result = new byte[blockPaddedCount];
        //    Array.Copy(decrypted, 0, result, 0, minimalDecryptableSize);

        //    return result;
        //}

        //private void ValidateInput(byte[] buffer, int offset, int count)
        //{
        //    if (offset < 0 || count < 0)
        //        throw new InvalidDataException("Offset and count can't be negative.");
        //    if (offset + count > buffer.Length)
        //        throw new InvalidDataException("Buffer too short.");
        //    if (count > Length - Position)
        //        throw new InvalidDataException($"Can't read {count} bytes from position {Position} in stream with length {Length}.");
        //}
    }
}