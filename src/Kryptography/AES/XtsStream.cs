using Kryptography.AES.XTS;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kryptography.AES
{
    public class XtsStream : KryptoStream
    {
        private XtsCryptoTransform _encryptor;
        private XtsCryptoTransform _decryptor;

        private bool _littleEndianId;

        public override int BlockSize => 128;
        public override int BlockSizeBytes => 16;
        public int SectorSize { get; protected set; }
        protected override int BlockAlign => SectorSize;

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

            Keys = new List<byte[]>();
            Keys.Add(key);
            IV = id;

            var xts = XtsContext.Create(key, id, littleEndianId, sectorSize);

            _encryptor = (XtsCryptoTransform)xts.CreateEncryptor();
            _decryptor = (XtsCryptoTransform)xts.CreateDecryptor();
        }

        public new void Dispose()
        {
            base.Dispose();

            _decryptor.Dispose();
            _encryptor.Dispose();
        }

        protected override void ProcessRead(long alignedPosition, int alignedCount, byte[] decryptedData, int decOffset)
        {
            var id = GetId(alignedPosition);

            Position = alignedPosition;

            var readData = new byte[alignedCount];
            _stream.Read(readData, 0, alignedCount);

            _decryptor.SectorId = id;
            _decryptor.TransformBlock(readData, 0, readData.Length, decryptedData, decOffset);
        }

        protected override void ProcessWrite(byte[] buffer, int offset, int count, long alignedPosition)
        {
            var id = GetId(alignedPosition);

            var encBuffer = new byte[count];
            _encryptor.SectorId = id;
            _encryptor.TransformBlock(buffer, offset, count, encBuffer, 0);

            Position = alignedPosition;
            _stream.Write(encBuffer, 0, encBuffer.Length);
        }

        private byte[] GetId(long alignedPosition)
        {
            var id = new byte[0x10];
            Array.Copy(IV, 0, id, 0, 0x10);

            if (alignedPosition >= SectorSize)
            {
                var count = alignedPosition / SectorSize;
                Increment(id, (int)count);
            }

            return id;
        }

        private void Increment(byte[] id, int count)
        {
            if (!_littleEndianId)
                for (int i = id.Length - 1; i >= 0; i--)
                {
                    if (count == 0)
                        break;

                    var check = id[i];
                    id[i] += (byte)count;
                    count >>= 8;

                    int off = 0;
                    while (i - off - 1 >= 0 && id[i - off] < check)
                    {
                        check = id[i - off - 1];
                        id[i - off - 1]++;
                        off++;
                    }
                }
            else
                for (int i = 0; i < id.Length; i++)
                {
                    if (count == 0)
                        break;

                    var check = id[i];
                    id[i] += (byte)count;
                    count >>= 8;

                    int off = 0;
                    while (i + off + 1 < id.Length && id[i + off] < check)
                    {
                        check = id[i + off + 1];
                        id[i + off + 1]++;
                        off++;
                    }
                }
        }
    }
}