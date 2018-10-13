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

        public override List<byte[]> Keys { get; }

        public override int KeySize => Keys?[0]?.Length ?? 0;

        public override byte[] IV => throw new NotImplementedException();

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _stream.Length;

        public override long Position { get => _stream.Position; set => Seek(value, SeekOrigin.Begin); }

        private Stream _stream;

        public XorStream(Stream input, string key, Encoding enc) : this(input, enc.GetBytes(key))
        {
        }

        public XorStream(Stream input, byte[] key)
        {
            _stream = input;

            Keys = new List<byte[]>();
            Keys.Add(key);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset + count >= buffer.Length)
                throw new InvalidDataException($"Buffer is too small.");

            var length = (int)Math.Max(0, Math.Min(count, Length - Position));

            var keyPos = Position % KeySize;
            for (int i = 0; i < length; i++)
            {
                buffer[offset + i] = (byte)(Keys[0][keyPos++] ^ _stream.ReadByte());
                if (keyPos >= KeySize)
                    keyPos = 0;
            }

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