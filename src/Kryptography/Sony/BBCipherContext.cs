using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace Kryptography.Sony
{
    public class BBCipherContext : SymmetricAlgorithm
    {
        byte[] _key;

        public BBCipherContext(byte[] key)
        {
            _key = new byte[key.Length];
            Array.Copy(key, _key, key.Length);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            ValidateKey(rgbKey);

            var key = _key ?? rgbKey;

            return new BBCipherTransform(key, true);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            ValidateKey(rgbKey);

            var key = _key ?? rgbKey;

            return new BBCipherTransform(key, false);
        }

        private void ValidateKey(byte[] key)
        {
            if (key == null && _key == null)
                throw new InvalidDataException("Key can't be null.");
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
