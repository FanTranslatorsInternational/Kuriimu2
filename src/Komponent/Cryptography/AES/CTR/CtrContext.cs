using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Komponent.Cryptography.AES.CTR
{
    public class AesCtr : SymmetricAlgorithm
    {
        public override byte[] Key { get; set; }
        public override byte[] IV { get; set; }
        public override PaddingMode Padding { get; set; }

        public new static SymmetricAlgorithm Create() => new AesCtr();

        public static SymmetricAlgorithm Create(byte[] key, byte[] iv)
        {
            ValidateInput(key, iv);

            return new AesCtr(key, iv);
        }

        static void ValidateInput(byte[] key, byte[] iv)
        {
            if (key.Length != 16 || iv.Length != 16)
                throw new InvalidOperationException("Key and IV need to be 16 bytes.");
        }

        protected AesCtr() { }

        protected AesCtr(byte[] key, byte[] iv)
        {
            Key = key;
            IV = iv;
        }

        public new ICryptoTransform CreateDecryptor()
        {
            ValidateKeyIV();

            return new CtrTransform(Key, IV);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            Key = rgbKey;
            IV = rgbIV;

            ValidateKeyIV();

            return new CtrTransform(Key, IV);
        }

        private void ValidateKeyIV()
        {
            if (Key == null || IV == null)
                throw new NotSupportedException("Key and IV can't be null.");
        }

        public new ICryptoTransform CreateEncryptor()
        {
            ValidateKeyIV();

            return new CtrTransform(Key, IV);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            Key = rgbKey;
            IV = rgbIV;

            ValidateKeyIV();

            return new CtrTransform(Key, IV);
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
