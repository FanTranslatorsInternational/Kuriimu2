using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Kryptography
{
    public class RotStream : KryptoStream
    {
        public override int BlockSize => 8;
        public override int BlockSizeBytes => 1;
        protected override int BlockAlign => BlockSizeBytes;

        public override List<byte[]> Keys { get; protected set; }
        public override int KeySize => Keys?[0]?.Length ?? 0;
        public override byte[] IV { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }

        public RotStream(byte[] input, byte key) : base(input)
        {
            Initialize(key);
        }

        public RotStream(Stream input, byte key) : base(input)
        {
            Initialize(key);
        }

        public RotStream(byte[] input, long offset, long length, byte key) : base(input, offset, length)
        {
            Initialize(key);
        }

        public RotStream(Stream input, long offset, long length, byte key) : base(input, offset, length)
        {
            Initialize(key);
        }

        private void Initialize(byte key)
        {
            Keys = new List<byte[]>();
            Keys.Add(new byte[] { key });
        }

        protected override void Decrypt(byte[] buffer, int offset, int count)
        {
            RotData(buffer, offset, count, Keys[0][0]);
        }

        protected override void Encrypt(byte[] buffer, int offset, int count)
        {
            RotData(buffer, offset, count, (byte)(0xFF - Keys[0][0]));
        }

        private void RotData(byte[] buffer, int offset, int count, byte rotBy)
        {
            var simdLength = Vector<byte>.Count;
            var rotBuffer = new byte[simdLength];
            for (int i = 0; i < simdLength; i++)
                rotBuffer[i] = rotBy;
            var vr = new Vector<byte>(rotBuffer);

            var j = 0;
            for (j = 0; j <= count - simdLength; j += simdLength)
            {
                var va = new Vector<byte>(buffer, j + offset);
                (va + vr).CopyTo(buffer, j + offset);
            }

            for (; j < count; ++j)
            {
                buffer[offset + j] += rotBy;
            }
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        //protected override void ProcessRead(long alignedPosition, int alignedCount, byte[] decryptedData, int decOffset)
        //{
        //    Position = alignedPosition;

        //    var readData = new byte[alignedCount];
        //    _stream.Read(readData, 0, alignedCount);

        //    for (int i = 0; i < alignedCount; i++)
        //        decryptedData[decOffset + i] = (byte)(readData[i] - Keys[0][0]);
        //}

        //protected override void ProcessWrite(byte[] buffer, int offset, int count, long alignedPosition)
        //{
        //    var encBuffer = new byte[count];
        //    for (int i = 0; i < count; i++)
        //        encBuffer[i] = (byte)(buffer[offset + i] + Keys[0][0]);

        //    Position = alignedPosition;
        //    _stream.Write(encBuffer, 0, count);
        //}
    }
}