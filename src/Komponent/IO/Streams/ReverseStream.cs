using System;
using System.IO;

namespace Komponent.IO.Streams
{
    /// <summary>
    /// Reverses the data in a stream.
    /// </summary>
    public class ReverseStream : Stream
    {
        private Stream _baseStream;

        /// <inheritdoc cref="CanRead"/>
        public override bool CanRead => true;

        /// <inheritdoc cref="CanWrite"/>
        public override bool CanWrite => true;

        /// <inheritdoc cref="CanSeek"/>
        public override bool CanSeek => true;

        /// <inheritdoc cref="Position"/>
        public override long Position { get; set; }

        /// <inheritdoc cref="Length"/>
        public override long Length { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ReverseStream"/>.
        /// </summary>
        /// <param name="baseStream">The stream to reverse.</param>
        /// <param name="length">The length of the reversed stream.</param>
        public ReverseStream(Stream baseStream, long length)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            Length = length;
        }

        /// <inheritdoc cref="Flush"/>
        public override void Flush()
        {
            _baseStream.Flush();
        }

        /// <inheritdoc cref="SetLength"/>
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

        /// <inheritdoc cref="Dispose(bool)"/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _baseStream = null;

            base.Dispose(disposing);
        }
    }
}
