using System;
using System.Collections.Generic;
using System.IO;

namespace Kryptography
{
    public class RotStream : KryptoStream
    {
        public override int BlockSize => 8;

        public override int BlockSizeBytes => 1;

        public override List<byte[]> Keys { get; }

        public override int KeySize => Keys?[0]?.Length ?? 0;

        public override byte[] IV => throw new NotImplementedException();

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _stream.Length;

        public override long Position { get => _stream.Position; set => Seek(value, SeekOrigin.Begin); }

        private Stream _stream;

        public RotStream(Stream input, byte n)
        {
            _stream = input;

            Keys = new List<byte[]>();
            Keys.Add(new byte[] { n });
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset + count >= buffer.Length)
                throw new InvalidDataException($"Buffer is too small.");

            var length = (int)Math.Max(0, Math.Min(count, Length - Position));

            for (int i = 0; i < length; i++)
                buffer[offset + i] = (byte)(_stream.ReadByte() - Keys[0][0]);

            return length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset + count >= buffer.Length)
                throw new InvalidDataException($"Buffer is too small.");

            for (int i = 0; i < count; i++)
                _stream.WriteByte((byte)(buffer[offset + i] + Keys[0][0]));
        }
    }
}