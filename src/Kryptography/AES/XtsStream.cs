using Kryptography.AES.XTS;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kryptography.AES
{
    public class XtsStream : KryptoStream
    {
        private AesXtsCryptoTransform _encryptor;
        private AesXtsCryptoTransform _decryptor;
        private bool _littleEndianId;

        public override int BlockSize => 128;
        public override int BlockSizeBytes => 16;
        public int SectorSize { get; protected set; }
        protected override int BlockAlign => BlockSizeBytes;
        protected override int SectorAlign => SectorSize;

        public override List<byte[]> Keys { get; protected set; }
        public override int KeySize => Keys?[0]?.Length ?? 0;
        public override byte[] IV { get; protected set; }

        public XtsStream(byte[] input, byte[] key, byte[] sectorId, bool littleEndianId = false, int sectorSize = 512) : base(input)
        {
            Initialize(key, sectorId, littleEndianId, sectorSize);
        }

        public XtsStream(Stream input, byte[] key, byte[] sectorId, bool littleEndianId = false, int sectorSize = 512) : base(input)
        {
            Initialize(key, sectorId, littleEndianId, sectorSize);
        }

        public XtsStream(byte[] input, long offset, long length, byte[] key, byte[] sectorId, bool littleEndianId = false, int sectorSize = 512) : base(input, offset, length)
        {
            Initialize(key, sectorId, littleEndianId, sectorSize);
        }

        public XtsStream(Stream input, long offset, long length, byte[] key, byte[] sectorId, bool littleEndianId = false, int sectorSize = 512) : base(input, offset, length)
        {
            Initialize(key, sectorId, littleEndianId, sectorSize);
        }

        private void Initialize(byte[] key, byte[] id, bool littleEndianId, int sectorSize)
        {
            _littleEndianId = littleEndianId;
            SectorSize = sectorSize;

            var xts = AesXts.Create(littleEndianId, sectorSize);

            _encryptor = (AesXtsCryptoTransform)xts.CreateEncryptor(key, id);
            _decryptor = (AesXtsCryptoTransform)xts.CreateDecryptor(key, id);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _encryptor.Dispose();
                _decryptor.Dispose();
            }
        }

        protected override void Decrypt(byte[] buffer, int offset, int count)
        {
            var iv = new byte[BlockSizeBytes];
            GetId(iv);

            _decryptor.SectorId = iv;
            _decryptor.TransformBlock(buffer, offset, count, buffer, offset);
        }

        protected override void Encrypt(byte[] buffer, int offset, int count)
        {
            var iv = new byte[BlockSizeBytes];
            GetId(iv);

            _encryptor.SectorId = iv;
            _encryptor.TransformBlock(buffer, offset, count, buffer, offset);
        }

        private void GetId(byte[] id)
        {
            Array.Copy(IV, 0, id, 0, 0x10);

            if (_baseStream.Position >= SectorSize)
            {
                var count = _baseStream.Position / SectorSize;
                id.Increment((int)count, _littleEndianId);
            }
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}