using System;
using System.Security.Cryptography;

namespace Kryptography.AES.XTS
{
    public class AesXts : SymmetricAlgorithm
    {
        private readonly int _sectorSize;
        private readonly bool _littleEndianId;
        private Aes _aes;

        public override byte[] Key { get; set; }
        public override byte[] IV { get; set; }

        /// <summary>
        /// Creates a AesXts
        /// </summary>
        /// <param name="littleEndianId">Defines if the id is little endian</param>
        /// <param name="sectorSize">Defines the size of a sector</param>
        public static AesXts Create(bool littleEndianId, int sectorSize)
        {
            return new AesXts(littleEndianId, sectorSize);
        }

        protected AesXts(bool littleEndianId, int sectorSize)
        {
            _littleEndianId = littleEndianId;
            _sectorSize = sectorSize;
            CreateAesContext();
        }

        private void CreateAesContext()
        {
            _aes = Aes.Create() ?? throw new ArgumentNullException(nameof(_aes));
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIv)
        {
            ValidateInput(rgbKey, rgbIv);

            Key = new byte[rgbKey.Length];
            Array.Copy(rgbKey, Key, rgbKey.Length);
            IV = new byte[rgbIv.Length];
            Array.Copy(rgbIv, IV, rgbIv.Length);

            var key1 = new byte[rgbKey.Length / 2];
            Array.Copy(rgbKey, key1, key1.Length);
            var key2 = new byte[rgbKey.Length / 2];
            Array.Copy(rgbKey, key1.Length, key2, 0, key2.Length);

            return new AesXtsCryptoTransform(
                _aes.CreateDecryptor(key1, null),
                _aes.CreateEncryptor(key2, null),
                IV,
                _sectorSize,
                _littleEndianId);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIv)
        {
            ValidateInput(rgbKey, rgbIv);

            Key = new byte[rgbKey.Length];
            Array.Copy(rgbKey, Key, rgbKey.Length);
            IV = new byte[rgbIv.Length];
            Array.Copy(rgbIv, IV, rgbIv.Length);

            var key1 = new byte[rgbKey.Length / 2];
            Array.Copy(rgbKey, key1, key1.Length);
            var key2 = new byte[rgbKey.Length / 2];
            Array.Copy(rgbKey, key1.Length, key2, 0, key2.Length);

            return new AesXtsCryptoTransform(
                _aes.CreateEncryptor(key1, null),
                _aes.CreateEncryptor(key2, null),
                IV,
                _sectorSize,
                _littleEndianId);
        }

        private void ValidateInput(byte[] key, byte[] iv)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (iv == null) throw new ArgumentNullException(nameof(iv));
            if (key.Length != 32 && key.Length != 64)
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