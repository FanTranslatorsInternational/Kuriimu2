using Kryptography.AES.CTR;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kryptography.AES
{
    public class CtrStream : KryptoStream
    {
        private CtrCryptoTransform _decryptor;
        private CtrCryptoTransform _encryptor;
        private bool _littleEndianCtr;

        public override int BlockSize => 128;
        public override int BlockSizeBytes => 16;
        protected override int BlockAlign => BlockSizeBytes;

        public override byte[] IV { get; protected set; }
        public override List<byte[]> Keys { get; protected set; }
        public override int KeySize => Keys?[0]?.Length ?? 0;

        public CtrStream(byte[] input, byte[] key, byte[] ctr, bool littleEndianCtr = false) : base(input)
        {
            Initialize(key, ctr, littleEndianCtr);
        }

        public CtrStream(Stream input, byte[] key, byte[] ctr, bool littleEndianCtr = false) : base(input)
        {
            Initialize(key, ctr, littleEndianCtr);
        }

        public CtrStream(byte[] input, long offset, long length, byte[] key, byte[] ctr, bool littleEndianCtr = false) : base(input, offset, length)
        {
            Initialize(key, ctr, littleEndianCtr);
        }

        public CtrStream(Stream input, long offset, long length, byte[] key, byte[] ctr, bool littleEndianCtr = false) : base(input, offset, length)
        {
            Initialize(key, ctr, littleEndianCtr);
        }

        private void Initialize(byte[] key, byte[] ctr, bool littleEndianCtr)
        {
            Keys = new List<byte[]>();
            Keys.Add(key);
            IV = ctr;

            _littleEndianCtr = littleEndianCtr;

            var aes = AesCtr.Create(false);
            _decryptor = (CtrCryptoTransform)aes.CreateDecryptor(key, ctr);
            _encryptor = (CtrCryptoTransform)aes.CreateEncryptor(key, ctr);
        }

        public new void Dispose()
        {
            base.Dispose();

            _encryptor.Dispose();
            _decryptor.Dispose();
        }

        protected override void ProcessRead(long alignedPosition, int alignedCount, byte[] decryptedData, int decOffset)
        {
            var iv = GetIV(alignedPosition);

            Position = alignedPosition;

            var readData = new byte[alignedCount];
            _stream.Read(readData, 0, alignedCount);

            _decryptor.IV = iv;
            _decryptor.TransformBlock(readData, 0, readData.Length, decryptedData, decOffset);
        }

        protected override void ProcessWrite(byte[] buffer, int offset, int count, long alignedPosition)
        {
            var iv = GetIV(alignedPosition);

            var encBuffer = new byte[count];
            _encryptor.IV = iv;
            _encryptor.TransformBlock(buffer, offset, count, encBuffer, 0);

            Position = alignedPosition;
            _stream.Write(encBuffer, 0, encBuffer.Length);
        }

        private byte[] GetIV(long alignedPosition)
        {
            var iv = new byte[0x10];
            Array.Copy(IV, 0, iv, 0, 0x10);

            if (alignedPosition >= 0x10)
            {
                var count = alignedPosition / 0x10;
                IncrementCtr(iv, (int)count);
            }

            return iv;
        }

        private void IncrementCtr(byte[] ctr, int count)
        {
            if (!_littleEndianCtr)
                for (int i = ctr.Length - 1; i >= 0; i--)
                {
                    if (count == 0)
                        break;

                    var check = ctr[i];
                    ctr[i] += (byte)count;
                    count >>= 8;

                    int off = 0;
                    while (i - off - 1 >= 0 && ctr[i - off] < check)
                    {
                        check = ctr[i - off - 1];
                        ctr[i - off - 1]++;
                        off++;
                    }
                }
            else
                for (int i = 0; i < ctr.Length; i++)
                {
                    if (count == 0)
                        break;

                    var check = ctr[i];
                    ctr[i] += (byte)count;
                    count >>= 8;

                    int off = 0;
                    while (i + off + 1 < ctr.Length && ctr[i + off] < check)
                    {
                        check = ctr[i + off + 1];
                        ctr[i + off + 1]++;
                        off++;
                    }
                }
        }
    }
}