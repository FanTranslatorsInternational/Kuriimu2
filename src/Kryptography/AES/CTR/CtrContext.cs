using System;
using System.Security.Cryptography;

namespace Kryptography.AES.CTR
{
    public class AesCtr : SymmetricAlgorithm
    {
        private byte[] _key;
        private bool _littleEndianCtr;
        public override byte[] Key { get => _key; set { _key = value; _cryptor = _aes.CreateEncryptor(_key, null); } }
        public override byte[] IV { get; set; }
        public override PaddingMode Padding { get; set; }

        new public static AesCtr Create() => new AesCtr(false);

        public static AesCtr Create(bool littleEndianCtr) => new AesCtr(littleEndianCtr);

        public static AesCtr Create(byte[] key, byte[] iv, bool littleEndianCtr)
        {
            ValidateInput(key, iv);

            return new AesCtr(key, iv, littleEndianCtr);
        }

        private static void ValidateInput(byte[] key, byte[] iv)
        {
            if (key.Length != 16 || iv.Length != 16)
                throw new InvalidOperationException("Key and IV need to be 16 bytes.");
        }

        private Aes _aes;
        private ICryptoTransform _cryptor;

        protected AesCtr(bool littleEndianCtr)
        {
            _littleEndianCtr = littleEndianCtr;
            CreateAesContext();
        }

        protected AesCtr(byte[] key, byte[] iv, bool littleEndianCtr)
        {
            Key = key;
            IV = iv;
            _littleEndianCtr = littleEndianCtr;

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

            return new CtrCryptoTransform(_cryptor, IV, _littleEndianCtr);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            Key = rgbKey;
            IV = rgbIV;

            ValidateKeyIV();

            return new CtrCryptoTransform(_cryptor, IV, _littleEndianCtr);
        }

        private void ValidateKeyIV()
        {
            if (Key == null || IV == null)
                throw new NotSupportedException("Key and IV can't be null.");
        }

        public new ICryptoTransform CreateEncryptor()
        {
            ValidateKeyIV();

            return new CtrCryptoTransform(_cryptor, IV, _littleEndianCtr);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            Key = rgbKey;
            IV = rgbIV;

            ValidateKeyIV();

            return new CtrCryptoTransform(_cryptor, IV, _littleEndianCtr);
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