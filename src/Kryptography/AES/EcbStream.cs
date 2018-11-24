using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Kryptography.AES
{
    public sealed class EcbStream : KryptoStream
    {
        private ICryptoTransform _decryptor;
        private ICryptoTransform _encryptor;

        public override int BlockSize => 128;
        public override int BlockSizeBytes => 16;
        protected override int BlockAlign => BlockSizeBytes;

        public override byte[] IV { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }

        public override List<byte[]> Keys { get; protected set; }
        public override int KeySize => Keys?[0]?.Length ?? 0;

        public EcbStream(byte[] input, byte[] key) : base(input)
        {
            Initialize(key);
        }

        public EcbStream(Stream input, byte[] key) : base(input)
        {
            Initialize(key);
        }

        public EcbStream(byte[] input, long offset, long length, byte[] key) : base(input, offset, length)
        {
            Initialize(key);
        }

        public EcbStream(Stream input, long offset, long length, byte[] key) : base(input, offset, length)
        {
            Initialize(key);
        }

        private void Initialize(byte[] key)
        {
            Keys = new List<byte[]>();
            Keys.Add(key);

            var aes = Aes.Create();
            aes.Padding = PaddingMode.None;
            aes.Mode = CipherMode.ECB;

            _decryptor = aes.CreateDecryptor(key, null);
            _encryptor = aes.CreateEncryptor(key, null);
        }

        public new void Dispose()
        {
            base.Dispose();

            _decryptor.Dispose();
            _encryptor.Dispose();
        }

        protected override int ProcessRead(long streamPos, byte[] buffer, int offset, int count)
        {
            return base.ProcessRead(streamPos, buffer, offset, count);
        }

        protected override void ProcessRead(long alignedPosition, int alignedCount, byte[] decryptedData, int decOffset)
        {
            Position = alignedPosition;

            var readData = new byte[alignedCount];
            _stream.Read(readData, 0, alignedCount);

            _decryptor.TransformBlock(readData, 0, readData.Length, decryptedData, decOffset);
        }

        protected override void ProcessWrite(byte[] buffer, int offset, int count, long alignedPosition)
        {
            var encBuffer = new byte[count];
            _encryptor.TransformBlock(buffer, offset, count, encBuffer, 0);

            Position = alignedPosition;
            _stream.Write(encBuffer, 0, count);
        }
    }
}