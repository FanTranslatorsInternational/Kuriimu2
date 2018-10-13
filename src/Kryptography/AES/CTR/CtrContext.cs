using System;
using System.Security.Cryptography;

namespace Kryptography.AES.CTR
{
    public class AesCtr : SymmetricAlgorithm
    {
        private byte[] _key;
        public override byte[] Key { get => _key; set { _key = value; _cryptor = _aes.CreateEncryptor(_key, null); } }
        public override byte[] IV { get; set; }
        public override PaddingMode Padding { get; set; }

        public new static SymmetricAlgorithm Create() => new AesCtr();

        public static SymmetricAlgorithm Create(byte[] key, byte[] iv)
        {
            ValidateInput(key, iv);

            return new AesCtr(key, iv);
        }

        private static void ValidateInput(byte[] key, byte[] iv)
        {
            if (key.Length != 16 || iv.Length != 16)
                throw new InvalidOperationException("Key and IV need to be 16 bytes.");
        }

        private Aes _aes;
        private ICryptoTransform _cryptor;

        protected AesCtr()
        {
            CreateAesContext();
        }

        protected AesCtr(byte[] key, byte[] iv)
        {
            Key = key;
            IV = iv;

            CreateAesContext();
        }

        private void CreateAesContext()
        {
            _aes = Aes.Create();
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;
        }

        public new ICryptoTransform CreateDecryptor()
        {
            ValidateKeyIV();

            return new CtrCryptoTransform(_cryptor, IV);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            Key = rgbKey;
            IV = rgbIV;

            ValidateKeyIV();

            return new CtrCryptoTransform(_cryptor, IV);
        }

        private void ValidateKeyIV()
        {
            if (Key == null || IV == null)
                throw new NotSupportedException("Key and IV can't be null.");
        }

        public new ICryptoTransform CreateEncryptor()
        {
            ValidateKeyIV();

            return new CtrCryptoTransform(_cryptor, IV);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            Key = rgbKey;
            IV = rgbIV;

            ValidateKeyIV();

            return new CtrCryptoTransform(_cryptor, IV);
        }

        public override void GenerateIV()
        {
            throw new NotImplementedException();
        }

        public override void GenerateKey()
        {
            throw new NotImplementedException();
        }
    }
}