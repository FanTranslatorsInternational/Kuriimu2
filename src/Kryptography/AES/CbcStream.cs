using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Kryptography.AES
{
    public class CbcStream : KryptoStream
    {
        private Aes _aes;

        public override int BlockSize => 128;
        public override int BlockSizeBytes => 16;
        protected override int BlockAlign => BlockSizeBytes;

        public override byte[] IV { get; protected set; }
        public override List<byte[]> Keys { get; protected set; }
        public override int KeySize => Keys?[0]?.Length ?? 0;

        public CbcStream(byte[] input, byte[] key, byte[] iv) : base(input)
        {
            Initialize(key, iv);
        }

        public CbcStream(Stream input, byte[] key, byte[] iv) : base(input)
        {
            Initialize(key, iv);
        }

        public CbcStream(byte[] input, long offset, long length, byte[] key, byte[] iv) : base(input, offset, length)
        {
            Initialize(key, iv);
        }

        public CbcStream(Stream input, long offset, long length, byte[] key, byte[] iv) : base(input, offset, length)
        {
            Initialize(key, iv);
        }

        private void Initialize(byte[] key, byte[] iv)
        {
            Keys = new List<byte[]>();
            Keys.Add(key);
            IV = iv;

            _aes = Aes.Create();
            _aes.Padding = PaddingMode.None;
            _aes.Mode = CipherMode.CBC;
        }

        public new void Dispose()
        {
            base.Dispose();

            _aes.Dispose();
        }

        protected override void ProcessRead(long alignedPosition, int alignedCount, byte[] decryptedData, int decOffset)
        {
            var iv = GetIV(alignedPosition);

            Position = alignedPosition;

            var readData = new byte[alignedCount];
            _stream.Read(readData, 0, alignedCount);

            _aes.CreateDecryptor(Keys[0], iv).TransformBlock(readData, 0, readData.Length, decryptedData, decOffset);
        }

        protected override void ProcessWrite(byte[] buffer, int offset, int count, long alignedPosition)
        {
            var iv = GetIV(alignedPosition);

            var encBuffer = new byte[count];
            _aes.CreateEncryptor(Keys[0], iv).TransformBlock(buffer, offset, count, encBuffer, 0);

            Position = alignedPosition;
            _stream.Write(encBuffer, 0, encBuffer.Length);
        }

        private byte[] GetIV(long alignedPosition)
        {
            var iv = new byte[0x10];

            if (alignedPosition < 0x10)
            {
                Array.Copy(IV, 0, iv, 0, 0x10);
            }
            else
            {
                Position = alignedPosition - 0x10;
                _stream.Read(iv, 0, 0x10);
            }

            return iv;
        }

        //private byte[] PeekLastValidVector()
        //{
        //    var ivBuffer = new byte[BlockSizeBytes];
        //    Array.Copy(IV, ivBuffer, BlockSizeBytes);

        //    var length = GetBlockCount(Length) * BlockSizeBytes;
        //    if (length == 0 || Position < BlockSizeBytes)
        //        return ivBuffer;

        //    var originalPosition = Position;
        //    _stream.Position = (GetCurrentBlock(Math.Min(length, Position)) - 1) * BlockSizeBytes;

        //    _stream.Read(ivBuffer, 0, ivBuffer.Length);

        //    _stream.Position = originalPosition;

        //    return ivBuffer;
        //}

        //private long GetBlocksBetween(long position)
        //{
        //    var offsetBlock = GetCurrentBlock(position);
        //    var lengthBlock = GetCurrentBlock(Length);
        //    if (Math.Max(offsetBlock, lengthBlock) - Math.Min(offsetBlock, lengthBlock) > 1)
        //        return Math.Max(offsetBlock, lengthBlock) - Math.Min(offsetBlock, lengthBlock) - 1;
        //    else
        //        return 0;
        //}

        //public override void SetLength(long value)
        //{
        //    throw new NotImplementedException();
        //}

        //public override void Write(byte[] buffer, int offset, int count)
        //{
        //    ValidateWrite(buffer, offset, count);

        //    if (count == 0) return;
        //    var readBuffer = GetInitializedReadBuffer(count, out var dataStart);

        //    var originalPosition = Position;
        //    Position -= dataStart;

        //    var stateVector = PeekLastValidVector();

        //    PeekOverlappingData(readBuffer, 0, count, stateVector);

        //    Array.Copy(buffer, offset, readBuffer, dataStart, count);

        //    var encBuffer = new byte[readBuffer.Length];
        //    _aes.CreateEncryptor(Keys[0], stateVector).TransformBlock(readBuffer, 0, readBuffer.Length, encBuffer, 0);

        //    _stream.Write(encBuffer, 0, encBuffer.Length);

        //    if (originalPosition + count > _length)
        //        _length = originalPosition + count;

        //    Seek(originalPosition + count, SeekOrigin.Begin);
        //}

        //private void ValidateWrite(byte[] buffer, int offset, int count)
        //{
        //    if (!CanWrite)
        //        throw new NotSupportedException("Write is not supported");
        //    if (Position < Length)
        //        throw new NotSupportedException("Rewriting data is not supported.");
        //    if (offset < 0 || count < 0)
        //        throw new ArgumentOutOfRangeException("Offset or count can't be negative.");
        //    if (offset + count > buffer.Length)
        //        throw new InvalidDataException("Buffer too short.");
        //}

        //private byte[] GetInitializedReadBuffer(int count, out long dataStart)
        //{
        //    var blocksBetweenLengthPosition = GetBlocksBetween(Position);
        //    var bytesIntoBlock = Position % BlockSizeBytes;

        //    dataStart = bytesIntoBlock;

        //    var bufferBlocks = GetBlockCount(bytesIntoBlock + count);
        //    if (Position >= Length)
        //    {
        //        bufferBlocks += blocksBetweenLengthPosition;
        //        dataStart += blocksBetweenLengthPosition * BlockSizeBytes;
        //    }

        //    var bufferLength = bufferBlocks * BlockSizeBytes;

        //    return new byte[bufferLength];
        //}

        //private void PeekOverlappingData(byte[] buffer, int offset, int count, byte[] stateVector)
        //{
        //    if (Position - offset < Length)
        //    {
        //        long originalPosition = Position;

        //        var readBuffer = Decrypt(Position - offset, (int)GetBlockCount(Math.Min(Length - (Position - offset), count)) * BlockSizeBytes, stateVector);
        //        Array.Copy(readBuffer, 0, buffer, 0, readBuffer.Length);

        //        Position = originalPosition;
        //    }
        //}
    }
}