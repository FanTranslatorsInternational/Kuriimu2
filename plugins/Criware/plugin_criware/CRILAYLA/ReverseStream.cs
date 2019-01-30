using System;
using System.IO;

namespace plugin_criware.CRILAYLA
{
    public class ReverseStream : Stream
    {
        private readonly Stream _baseStream;

        public ReverseStream(Stream baseStream)
        {
            // Assign private members
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        }

        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        public override long Length => _baseStream.Length;
        public override bool CanRead => _baseStream.CanRead;
        public override bool CanWrite => _baseStream.CanWrite;
        public override bool CanSeek => _baseStream.CanSeek;
        public override void Flush() => _baseStream.Flush();

        public override void SetLength(long value) => _baseStream.SetLength(value);

        public override int Read(byte[] buffer, int offset, int count)
        {
            Position -= count;
            var read = _baseStream.Read(buffer, offset - count, (int)Math.Min(count, _baseStream.Length - Position));
            Position -= read;
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Position -= count;
            _baseStream.Write(buffer, offset - count, count);
            Position -= count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: return Position = offset;
                case SeekOrigin.Current: return Position += offset;
                case SeekOrigin.End: return Position = _baseStream.Length - offset;
            }
            throw new ArgumentException("origin is invalid");
        }
    }
}
