using System;
using System.IO;

namespace Kompression.IO
{
    /// <summary>
    /// Concatenates two streams into one.
    /// </summary>
    public class ConcatStream : Stream
    {
        private Stream _baseStream1;
        private Stream _baseStream2;

        /// <inheritdoc cref="CanRead"/>
        public override bool CanRead => true;

        /// <inheritdoc cref="CanSeek"/>
        public override bool CanSeek => true;

        /// <inheritdoc cref="CanWrite"/>
        public override bool CanWrite => false;

        /// <inheritdoc cref="Length"/>
        public override long Length => _baseStream1.Length + _baseStream2.Length;

        /// <inheritdoc cref="Position"/>
        public override long Position { get; set; }

        /// <summary>
        /// Create a new instance of <see cref="ConcatStream"/>.
        /// </summary>
        /// <param name="baseStream1">The first stream in the concatenation.</param>
        /// <param name="baseStream2">The second stream in the concatenation.</param>
        public ConcatStream(Stream baseStream1, Stream baseStream2)
        {
            _baseStream1 = baseStream1 ?? throw new ArgumentNullException(nameof(baseStream1));
            _baseStream2 = baseStream2 ?? throw new ArgumentNullException(nameof(baseStream2));
        }

        /// <inheritdoc cref="Flush"/>
        public override void Flush()
        {
            _baseStream1.Flush();
            _baseStream2.Flush();
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

        /// <inheritdoc cref="SetLength"/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="Read"/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position >= Length)
                return 0;

            int readBytes;
            var cappedCount = readBytes = (int)Math.Min(Length - Position, count);

            if (Position < _baseStream1.Length)
            {
                var toRead = Math.Min(cappedCount, (int)(_baseStream1.Length - Position));

                var bkPos = _baseStream1.Position;
                _baseStream1.Position = Position;
                _baseStream1.Read(buffer, offset, toRead);
                _baseStream1.Position = bkPos;

                offset += toRead;
                Position += toRead;
                cappedCount -= toRead;
            }

            if (cappedCount > 0)
            {
                var toRead = Math.Min(cappedCount, (int)(_baseStream2.Length - (Position - _baseStream1.Length)));

                var bkPos = _baseStream2.Position;
                _baseStream2.Position = Position - _baseStream1.Length;
                _baseStream2.Read(buffer, offset, toRead);
                _baseStream2.Position = bkPos;

                Position += toRead;
            }

            return readBytes;
        }

        /// <inheritdoc cref="Write"/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="Dispose(bool)"/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _baseStream1 = null;
                _baseStream2 = null;
            }

            base.Dispose(disposing);
        }
    }
}
