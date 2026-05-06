using System.Security.Cryptography;

namespace Kryptography.Encryption.AES.CTR
{
    public class AesCtr : SymmetricAlgorithm
    {
        private readonly Aes _aes;
        private readonly bool _littleEndianCtr;

        public override byte[] Key { get; set; } = [];
        public override byte[] IV { get; set; } = [];

        public new static AesCtr Create() => new(false);

        public static AesCtr Create(bool littleEndianCtr) => new(littleEndianCtr);

        protected AesCtr(bool littleEndianCtr)
        {
            _littleEndianCtr = littleEndianCtr;

            _aes = Aes.Create();
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIv)
        {
            ValidateInput(rgbKey, rgbIv);

            Key = new byte[rgbKey.Length];
            Array.Copy(rgbKey, Key, rgbKey.Length);
            IV = new byte[rgbIv!.Length];
            Array.Copy(rgbIv, IV, rgbIv.Length);

            return new AesCtrCryptoTransform(_aes.CreateEncryptor(Key, null), IV, _littleEndianCtr);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIv)
        {
            ValidateInput(rgbKey, rgbIv);

            Key = new byte[rgbKey.Length];
            Array.Copy(rgbKey, Key, rgbKey.Length);
            IV = new byte[rgbIv!.Length];
            Array.Copy(rgbIv, IV, rgbIv.Length);

            return new AesCtrCryptoTransform(_aes.CreateEncryptor(Key, null), IV, _littleEndianCtr);
        }

        private static void ValidateInput(byte[] key, byte[]? iv)
        {
            ArgumentNullException.ThrowIfNull(iv);

            if (key.Length / 4 < 4 || key.Length / 4 > 8 || key.Length % 4 > 0)
                throw new InvalidOperationException("Key has invalid length.");
            if (iv.Length != 16)
                throw new InvalidOperationException("IV needs a length of 16 bytes.");
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