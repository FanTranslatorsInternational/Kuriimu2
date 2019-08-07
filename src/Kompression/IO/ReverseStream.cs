using System;
using System.IO;
using Kompression.Exceptions;

namespace Kompression.IO
{
    public class ReverseStream : Stream
    {
        private Stream _baseStream;

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
            if (Position - count < 0)
                throw new BeginningOfStreamException();

            Position -= count;
            var read = _baseStream.Read(buffer, offset, (int)Math.Min(count, _baseStream.Length - Position));
            Position -= count;
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Position - count < 0)
                throw new BeginningOfStreamException();

            Position -= count;
            _baseStream.Write(buffer, offset, count);
            Position -= count;
        }

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _baseStream = null;
            base.Dispose(disposing);
        }
    }
}
