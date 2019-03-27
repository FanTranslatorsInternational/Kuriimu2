using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Kryptography
{
    public sealed class RotStream : Stream
    {
        private Stream _baseStream;

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length;

        public override long Position { get => _baseStream.Position; set => Seek(value, SeekOrigin.Begin); }

        private byte _key;

        public RotStream(Stream input, byte key)
        {
            _baseStream = input;
            _key = key;
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
                buffer[offset + j] += rotBy;
        }

        public override void Flush() => _baseStream.Flush();

        public override void SetLength(long value) => _baseStream.SetLength(value);

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException("Can't seek stream.");

            return _baseStream.Seek(offset, origin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Can't read from stream.");

            _baseStream.Read(buffer, offset, count);
            RotData(buffer, offset, count, (byte)(0x100 - _key));
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            RotData(buffer, offset, count, _key);
            _baseStream.Write(buffer, offset, count);
        }
    }
}