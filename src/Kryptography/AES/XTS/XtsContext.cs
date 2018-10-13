using System;
using System.Security.Cryptography;

namespace Kryptography.AES.XTS
{
    public class XtsContext : SymmetricAlgorithm
    {
        private byte[] _sectorId;
        private byte[] _totalKey;
        private byte[] _key1;
        private byte[] _key2;
        public override byte[] Key { get => _totalKey; set { ValidateKey(value); _totalKey = value; SplitKey(); } }
        public override byte[] IV { get => _sectorId; set { ValidateIV(value); _sectorId = value; } }
        private int _sectorSize;
        private bool _littleEndianId;
        private Aes _aes;

        /// <summary>
        /// Creates a XtsContext
        /// </summary>
        /// <param name="sectorSize">Defines the size of a sector</param>
        public static XtsContext Create(bool littleEndianId, int sectorSize)
        {
            return new XtsContext(littleEndianId, sectorSize);
        }

        /// <summary>
        /// Creates a XtsContext
        /// </summary>
        /// <param name="keys">Contains both Xts Keys. Needs to be 32 or 64 bytes.</param>
        /// <param name="sectorId">Contains sectionId as byte[]</param>
        /// <param name="littleEndianId">Defines how the Id gets increment later on</param>
        /// <param name="sectorSize">Defines the size of a sector</param>
        public static XtsContext Create(byte[] keys, byte[] sectorId, bool littleEndianId, int sectorSize)
        {
            return new XtsContext(keys, sectorId, littleEndianId, sectorSize);
        }

        protected XtsContext(bool littleEndianId, int sectorSize)
        {
            _littleEndianId = littleEndianId;
            _sectorSize = sectorSize;
            CreateAesContext();
        }

        protected XtsContext(byte[] keys, byte[] iv, bool littleEndianId, int sectorSize)
        {
            Key = keys;
            IV = iv;
            _sectorSize = sectorSize;
            _littleEndianId = littleEndianId;

            CreateAesContext();
        }

        private void CreateAesContext()
        {
            _aes = Aes.Create();
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;
        }

        private void SplitKey()
        {
            var keyLength = _totalKey.Length / 2;

            if (_key1 == null)
                _key1 = new byte[keyLength];
            if (_key2 == null)
                _key2 = new byte[keyLength];

            Buffer.BlockCopy(_totalKey, 0, _key1, 0, keyLength);
            Buffer.BlockCopy(_totalKey, keyLength, _key2, 0, keyLength);
        }

        private void ValidateKey(byte[] keys)
        {
            if (keys.Length != 32 && keys.Length != 64)
                throw new InvalidOperationException("Keys needs to be 32 or 64 bytes.");
        }

        private void ValidateIV(byte[] iv)
        {
            if (iv.Length != 16)
                throw new InvalidOperationException("IV needs to be 16 bytes.");
        }

        public override ICryptoTransform CreateDecryptor()
        {
            return new XtsCryptoTransform(_aes.CreateDecryptor(_key1, null), _aes.CreateEncryptor(_key2, null), IV, _sectorSize, _littleEndianId);
        }

        public override ICryptoTransform CreateEncryptor()
        {
            return new XtsCryptoTransform(_aes.CreateEncryptor(_key1, null), _aes.CreateEncryptor(_key2, null), IV, _sectorSize, _littleEndianId);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            Key = rgbKey;
            IV = rgbIV;

            return CreateDecryptor();
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            Key = rgbKey;
            IV = rgbIV;

            return CreateEncryptor();
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