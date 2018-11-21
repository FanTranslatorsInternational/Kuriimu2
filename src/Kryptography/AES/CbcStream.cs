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
    }
}