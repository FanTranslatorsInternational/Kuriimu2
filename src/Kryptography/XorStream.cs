using System;
using System.IO;
using System.Numerics;

namespace Kryptography
{
    public class XorStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly byte[] _key;

        public override bool CanRead => _baseStream.CanRead && true;

        public override bool CanSeek => _baseStream.CanSeek && true;

        public override bool CanWrite => _baseStream.CanWrite && true;

        public override long Length => _baseStream.Length;

        public override long Position { get; set; }

        public XorStream(Stream input, byte[] key)
        {
            _baseStream = input;
            _key = new byte[key.Length];
            Array.Copy(key, _key, key.Length);
        }

        public override void Flush() => _baseStream.Flush();

        public override void SetLength(long value)
        {
            if (!CanWrite || !CanSeek)
                throw new NotSupportedException("Can't set length of stream.");
            if (value < 0)
                throw new IOException("Length can't be smaller than 0.");

            if (value > Length)
            {
                var bkPosThis = Position;
                var bkPosBase = _baseStream.Position;

                var startPos = Math.Max(_baseStream.Length, Length);
                var newDataLength = value - startPos;
                var written = 0;
                var newData = new byte[0x10000];
                while (written < newDataLength)
                {
                    Position = startPos;
                    var toWrite = (int)Math.Min(0x10000, newDataLength - written);
                    Write(newData, 0, toWrite);
                    written += toWrite;
                    startPos += toWrite;
                }

                _baseStream.Position = bkPosBase;
                Position = bkPosThis;
            }
            else
                _baseStream.SetLength(value);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException("Can't seek stream.");

            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }

            return Position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Can't read from stream.");

            if (Position >= Length)
                return 0;

            var bkPos = _baseStream.Position;
            _baseStream.Position = Position;

            var length = (int)Math.Min(count, Length - Position);
            _baseStream.Read(buffer, offset, length);
            _baseStream.Position = bkPos;

            XorData(buffer, offset, length, _key);

            Position += length;
            return length;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            var internalBuffer = new byte[count];
            Array.Copy(buffer, offset, internalBuffer, 0, count);
            XorData(internalBuffer, offset, count, _key);

            var bkPos = _baseStream.Position;
            _baseStream.Position = Position;
            _baseStream.Write(internalBuffer, 0, count);
            _baseStream.Position = bkPos;

            Position += count;
        }

        private void XorData(byte[] buffer, int offset, int count, byte[] key)
        {
            int j;

            var xorBuffer = new byte[count];
            FillXorBuffer(xorBuffer, Position, key);

            var simdLength = Vector<byte>.Count;
            for (j = 0; j <= count - simdLength; j += simdLength)
            {
                var va = new Vector<byte>(buffer, j + offset);
                var vb = new Vector<byte>(xorBuffer, j);
                (va ^ vb).CopyTo(buffer, j + offset);
            }

            for (; j < count; ++j)
                buffer[offset + j] = (byte)(buffer[offset + j] ^ xorBuffer[j]);
        }

        protected virtual void FillXorBuffer(byte[] fill, long pos, byte[] key)
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
    }
}