using System.Security.Cryptography;

namespace Kryptography.Encryption.Sony.BBCipher
{
    public class BbCipherContext : SymmetricAlgorithm
    {
        private readonly byte[] _headerKey;
        private readonly byte[] _vkey;

        private readonly int _type;
        private readonly int _seed;

        public BbCipherContext(byte[] headerKey, byte[] vkey, int seed, int cipherType)
        {
            _headerKey = new byte[headerKey.Length];
            Array.Copy(headerKey, _headerKey, headerKey.Length);

            _vkey = new byte[vkey.Length];
            Array.Copy(vkey, _vkey, vkey.Length);

            _type = cipherType;
            _seed = seed;
        }

        public override ICryptoTransform CreateDecryptor()
        {
            return CreateDecryptor(null!, null);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIv)
        {
            return new BbCipherTransform(_headerKey, _vkey, _seed, _type, true);
        }

        public override ICryptoTransform CreateEncryptor()
        {
            return CreateEncryptor(null!, null);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIv)
        {
            return new BbCipherTransform(_headerKey, _vkey, _seed, _type, false);
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