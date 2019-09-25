using System;
using System.IO;

namespace Kompression.IO
{
    public class ReverseStream : Stream
    {
        private Stream _baseStream;
        private readonly long _length;

        public ReverseStream(Stream baseStream, long length)
        {
            // Assign private members
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _length = length;
        }

        public override long Position { get; set; }
        public override long Length => _length;
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => true;

        public override void Flush() { }

        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc cref="Read"/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position < 0)
                throw new EndOfStreamException();

            var toRead = (int)Math.Min(count, Length - Position);

            var bkPos = _baseStream.Position;
            _baseStream.Position = Length - Position - toRead;
            _baseStream.Read(buffer, offset, toRead);
            _baseStream.Position = bkPos;

            Array.Reverse(buffer, offset, toRead);

            Position += toRead;
            return toRead;
        }

        /// <inheritdoc cref="Write"/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Position < 0)
                throw new EndOfStreamException();

            var toRead = (int)Math.Min(count, Length - Position);

            var reverseBuffer = new byte[toRead];
            Array.Copy(buffer, offset, reverseBuffer, 0, toRead);
            Array.Reverse(reverseBuffer);

            var bkPos = _baseStream.Position;
            _baseStream.Position = Length - Position - toRead;
            _baseStream.Write(reverseBuffer, 0, toRead);
            _baseStream.Position = bkPos;

            Position += toRead;
        }

        /// <inheritdoc cref="Seek"/>
        public override long Seek(long offset, SeekOrigin origin)
        {
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

        /// <inheritdoc cref="Dispose"/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _baseStream = null;
            base.Dispose(disposing);
        }
    }
}
