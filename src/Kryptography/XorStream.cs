using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Kryptography
{
    public class XorStream : KryptoStream
    {
        public override int BlockSize => 8;
        public override int BlockSizeBytes => 1;
        protected override int BlockAlign => BlockSizeBytes;
        protected override int SectorAlign => BlockSizeBytes;

        public override List<byte[]> Keys { get; protected set; }
        public override int KeySize => Keys?[0]?.Length ?? 0;
        public override byte[] IV { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }

        public XorStream(byte[] input, byte[] key) : base(input)
        {
            Initialize(key);
        }

        public XorStream(Stream input, byte[] key) : base(input)
        {
            Initialize(key);
        }

        public XorStream(byte[] input, long offset, long length, byte[] key) : base(input, offset, length)
        {
            Initialize(key);
        }

        public XorStream(Stream input, long offset, long length, byte[] key) : base(input, offset, length)
        {
            Initialize(key);
        }

        private void Initialize(byte[] key)
        {
            Keys = new List<byte[]>();
            Keys.Add(key);
        }

        protected override void Decrypt(byte[] buffer, int offset, int count)
        {
            XORData(buffer, offset, count, Keys[0]);
        }

        protected override void Encrypt(byte[] buffer, int offset, int count)
        {
            XORData(buffer, offset, count, Keys[0]);
        }

        private void XORData(byte[] buffer, int offset, int count, byte[] key)
        {
            var xorBuffer = new byte[count];
            FillXorBuffer(xorBuffer, _baseStream.Position, key);

            var simdLength = Vector<byte>.Count;
            var j = 0;
            for (j = 0; j <= count - simdLength; j += simdLength)
            {
                var va = new Vector<byte>(buffer, j + offset);
                var vb = new Vector<byte>(xorBuffer, j);
                (va ^ vb).CopyTo(buffer, j + offset);
            }

            for (; j < count; ++j)
            {
                buffer[offset + j] = (byte)(buffer[offset + j] ^ xorBuffer[j]);
            }
        }

        private void FillXorBuffer(byte[] fill, long pos, byte[] key)
        {
            var written = 0;
            while (written < fill.Length)
            {
                var keyOffset = (int)(pos % key.Length);
                var size = Math.Min(key.Length - keyOffset, fill.Length - written);

                Array.Copy(key, keyOffset, fill, written, size);

                written += size;
                pos += size;
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