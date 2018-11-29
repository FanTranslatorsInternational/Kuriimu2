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

        protected override int BufferSize => 0x10;

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

        protected override void Decrypt(byte[] buffer, int offset, int count)
        {
            _decryptor.TransformBlock(buffer, offset, count, buffer, offset);
        }

        protected override void Encrypt(byte[] buffer, int offset, int count)
        {
            _encryptor.TransformBlock(buffer, offset, count, buffer, offset);
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