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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _aes.Dispose();
            }
        }

        protected override void Decrypt(byte[] buffer, int offset, int count)
        {
            var iv = new byte[BlockSizeBytes];
            GetIV(iv);

            _aes.CreateDecryptor(Keys[0], iv).TransformBlock(buffer, offset, count, buffer, offset);
        }

        protected override void Encrypt(byte[] buffer, int offset, int count)
        {
            var iv = new byte[BlockSizeBytes];
            GetIV(iv);

            _aes.CreateEncryptor(Keys[0], iv).TransformBlock(buffer, offset, count, buffer, offset);
        }

        private void GetIV(byte[] iv)
        {
            if (_baseStream.Position < BlockSizeBytes)
            {
                Array.Copy(IV, 0, iv, 0, BlockSizeBytes);
            }
            else
            {
                _baseStream.Position -= BlockSizeBytes;
                _baseStream.Read(iv, 0, BlockSizeBytes);
            }
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}