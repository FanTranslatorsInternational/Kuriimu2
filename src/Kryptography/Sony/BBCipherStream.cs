using Kryptography.Sony.BBCipher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kryptography.Sony
{
    public sealed class BBCipherStream : KryptoStream
    {
        public override int BlockSize => 128;

        public override int BlockSizeBytes => 16;

        public override List<byte[]> Keys { get; protected set; }

        public override int KeySize => Keys[0].Length;

        public override byte[] IV { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }

        protected override int BlockAlign => 0x10;
        protected override int SectorAlign => 0x800;

        BBCipherTransform _decryptor;
        BBCipherTransform _encryptor;

        public BBCipherStream(Stream input, byte[] key, byte[] vkey, int seed, int cipher_type) : base(input)
        {
            Initialize(key, vkey, seed, cipher_type);
        }

        public BBCipherStream(byte[] input, byte[] key, byte[] vkey, int seed, int cipher_type) : base(input)
        {
            Initialize(key, vkey, seed, cipher_type);
        }

        public BBCipherStream(Stream input, long offset, long length, byte[] key, byte[] vkey, int seed, int cipher_type) : base(input, offset, length)
        {
            Initialize(key, vkey, seed, cipher_type);
        }

        public BBCipherStream(byte[] input, long offset, long length, byte[] key, byte[] vkey, int seed, int cipher_type) : base(input, offset, length)
        {
            Initialize(key, vkey, seed, cipher_type);
        }

        private void Initialize(byte[] key, byte[] vkey, int seed, int cipher_type)
        {
            var context = new BBCipherContext(key, vkey, seed, cipher_type);

            _decryptor = (BBCipherTransform)context.CreateDecryptor();
            _encryptor = (BBCipherTransform)context.CreateEncryptor();
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        protected override void Decrypt(byte[] buffer, int offset, int count)
        {
            _decryptor.Seed = (int)(_baseStream.Position / 0x10 + 1);
            _decryptor.TransformBlock(buffer, offset, count, buffer, offset);
        }

        protected override void Encrypt(byte[] buffer, int offset, int count)
        {
            _encryptor.Seed = (int)(_baseStream.Position / 0x10 + 1);
            _encryptor.TransformBlock(buffer, offset, count, buffer, offset);
        }
    }
}
