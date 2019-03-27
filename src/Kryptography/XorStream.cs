using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Kryptography
{
    public class XorStream : Stream
    {
        private Stream _baseStream;
        private byte[] _key;

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length;

        public override long Position { get => _baseStream.Position; set => Seek(value, SeekOrigin.Begin); }

        public XorStream(Stream input, byte[] key)
        {
            _baseStream = input;
            _key = key;
        }

        private void XorData(byte[] buffer, int offset, int count, byte[] key)
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
            XorData(buffer, offset, count, _key);
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            XorData(buffer, offset, count, _key);
            _baseStream.Write(buffer, offset, count);
        }
    }
}