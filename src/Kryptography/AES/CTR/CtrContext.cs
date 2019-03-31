using System;
using System.Security.Cryptography;

namespace Kryptography.AES.CTR
{
    public class AesCtr : SymmetricAlgorithm
    {
        private Aes _aes;
        //private ICryptoTransform _cryptor;

        private bool _littleEndianCtr;

        public override byte[] Key { get; set; }
        public override byte[] IV { get; set; }
        //public override byte[] Key { get => _key; set { _key = value; _cryptor = _aes.CreateEncryptor(_key, null); } }
        //public override byte[] IV { get; set; }
        //public override PaddingMode Padding { get; set; }

        new public static AesCtr Create() => new AesCtr(false);

        public static AesCtr Create(bool littleEndianCtr) => new AesCtr(littleEndianCtr);

        //public static AesCtr Create(byte[] key, byte[] iv, bool littleEndianCtr)
        //{
        //    ValidateInput(key, iv);

        //    return new AesCtr(key, iv, littleEndianCtr);
        //}

        private static void ValidateInput(byte[] key, byte[] iv)
        {
            if (key.Length != 16 || iv.Length != 16)
                throw new InvalidOperationException("Key and IV need to be 16 bytes.");
        }

        protected AesCtr(bool littleEndianCtr)
        {
            _littleEndianCtr = littleEndianCtr;
            CreateAesContext();
        }

        //protected AesCtr(byte[] key, byte[] iv, bool littleEndianCtr)
        //{
        //    Key = key;
        //    IV = iv;
        //    _littleEndianCtr = littleEndianCtr;

        //    CreateAesContext();
        //}

        private void CreateAesContext()
        {
            _aes = Aes.Create();
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;
        }

        //public new ICryptoTransform CreateDecryptor()
        //{
        //    ValidateKeyIV();

        //    return new CtrCryptoTransform(_cryptor, IV, _littleEndianCtr);
        //}

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            if (rgbKey == null || rgbIV == null)
                throw new InvalidOperationException("Key and IV can't be null.");
            if (rgbKey.Length / 4 < 4 || rgbKey.Length / 4 > 8 || rgbKey.Length % 4 > 0)
                throw new InvalidOperationException("Invalid key length.");
            if (rgbIV.Length / 4 < 4 || rgbIV.Length / 4 > 8 || rgbIV.Length % 4 > 0)
                throw new InvalidOperationException("Invalid IV length.");

            Key = new byte[rgbKey.Length];
            Array.Copy(rgbKey, Key, rgbKey.Length);
            IV = new byte[rgbIV.Length];
            Array.Copy(rgbIV, IV, rgbIV.Length);

            //ValidateKeyIV();

            return new CtrCryptoTransform(_aes.CreateEncryptor(Key, null), IV, _littleEndianCtr);
        }

        //private void ValidateKeyIV()
        //{
        //    if (Key == null || IV == null)
        //        throw new NotSupportedException("Key and IV can't be null.");
        //}

        //public new ICryptoTransform CreateEncryptor()
        //{
        //    ValidateKeyIV();

        //    return new CtrCryptoTransform(_cryptor, IV, _littleEndianCtr);
        //}

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            if (rgbKey == null || rgbIV == null)
                throw new InvalidOperationException("Key and IV can't be null.");
            if (rgbKey.Length / 4 < 4 || rgbKey.Length / 4 > 8 || rgbKey.Length % 4 > 0)
                throw new InvalidOperationException("Invalid key length.");
            if (rgbIV.Length / 4 < 4 || rgbIV.Length / 4 > 8 || rgbIV.Length % 4 > 0)
                throw new InvalidOperationException("Invalid IV length.");

            Key = new byte[rgbKey.Length];
            Array.Copy(rgbKey, Key, rgbKey.Length);
            IV = new byte[rgbIV.Length];
            Array.Copy(rgbIV, IV, rgbIV.Length);

            //ValidateKeyIV();

            return new CtrCryptoTransform(_aes.CreateEncryptor(Key, null), IV, _littleEndianCtr);
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