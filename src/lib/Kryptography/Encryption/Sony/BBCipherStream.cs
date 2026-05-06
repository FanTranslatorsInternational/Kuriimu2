using Kryptography.Encryption.Sony.BBCipher;

namespace Kryptography.Encryption.Sony
{
    public sealed class BbCipherStream : KryptoStream
    {
        public override int BlockSize => 128;

        public override int BlockSizeBytes => 16;

        public override List<byte[]> Keys { get; protected set; } = [];

        public override int KeySize => Keys[0].Length;

        public override byte[] IV { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }

        protected override int BlockAlign => 0x10;
        protected override int SectorAlign => 0x800;

        private BbCipherTransform? _decryptor;
        private BbCipherTransform? _encryptor;

        public BbCipherStream(Stream input, byte[] key, byte[] vkey, int seed, int cipherType) : base(input)
        {
            Initialize(key, vkey, seed, cipherType);
        }

        public BbCipherStream(byte[] input, byte[] key, byte[] vkey, int seed, int cipherType) : base(input)
        {
            Initialize(key, vkey, seed, cipherType);
        }

        public BbCipherStream(Stream input, long offset, long length, byte[] key, byte[] vkey, int seed, int cipherType) : base(input, offset, length)
        {
            Initialize(key, vkey, seed, cipherType);
        }

        public BbCipherStream(byte[] input, long offset, long length, byte[] key, byte[] vkey, int seed, int cipherType) : base(input, offset, length)
        {
            Initialize(key, vkey, seed, cipherType);
        }

        private void Initialize(byte[] key, byte[] vkey, int seed, int cipherType)
        {
            var context = new BbCipherContext(key, vkey, seed, cipherType);

            _decryptor = (BbCipherTransform)context.CreateDecryptor();
            _encryptor = (BbCipherTransform)context.CreateEncryptor();
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
            ArgumentNullException.ThrowIfNull(_decryptor);

            _decryptor.Seed = (int)(BaseStream.Position / 0x10 + 1);
            _decryptor.TransformBlock(buffer, offset, count, buffer, offset);
        }

        protected override void Encrypt(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(_encryptor);

            _encryptor.Seed = (int)(BaseStream.Position / 0x10 + 1);
            _encryptor.TransformBlock(buffer, offset, count, buffer, offset);
        }
    }
}
