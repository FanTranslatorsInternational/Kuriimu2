using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kryptography.XOR
{
    public class XorStream : KryptoStream
    {
        public override int BlockSize => 8;
        public override int BlockSizeBytes => 1;
        protected override int BlockAlign => BlockSizeBytes;

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

        protected override void ProcessRead(long alignedPosition, int alignedCount, byte[] decryptedData, int decOffset)
        {
            Position = alignedPosition;

            var keyPos = Position % KeySize;
            for (int i = 0; i < alignedCount; i++)
            {
                decryptedData[decOffset + i] = (byte)(_stream.ReadByte() ^ Keys[0][keyPos++]);
                if (keyPos >= KeySize)
                    keyPos = 0;
            }
        }

        protected override void ProcessWrite(byte[] buffer, int offset, int count, long alignedPosition)
        {
            Position = alignedPosition;

            var keyPos = Position % KeySize;
            for (int i = 0; i < count; i++)
            {
                _stream.WriteByte((byte)(buffer[offset + i] ^ Keys[0][keyPos++]));
                if (keyPos >= KeySize)
                    keyPos = 0;
            }
        }
    }
}