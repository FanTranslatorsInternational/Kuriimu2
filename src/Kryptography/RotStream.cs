using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Kryptography
{
    public sealed class RotStream : Stream
    {
        private Stream _baseStream;

        public override bool CanRead => _baseStream.CanRead && true;

        public override bool CanSeek => _baseStream.CanSeek && true;

        public override bool CanWrite => _baseStream.CanWrite && true;

        public override long Length => _baseStream.Length;

        public override long Position { get; set; }

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

            var bkPos = _baseStream.Position;
            _baseStream.Position = Position;
            _baseStream.Read(buffer, offset, count);
            _baseStream.Position = bkPos;

            RotData(buffer, offset, count, (byte)(0x100 - _key));

            Position += count;
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            RotData(buffer, offset, count, _key);

            var bkPos = _baseStream.Position;
            _baseStream.Position = Position;
            _baseStream.Write(buffer, offset, count);
            _baseStream.Position = bkPos;

            Position += count;
        }
    }
}