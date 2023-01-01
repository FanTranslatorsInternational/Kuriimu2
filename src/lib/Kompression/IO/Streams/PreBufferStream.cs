using System;
using System.IO;
using Kontract;

namespace Kompression.IO.Streams
{
    class PreBufferStream : Stream
    {
        private readonly int _preBufferSize;
        private readonly byte _value;
        private Stream _baseStream;

        /// <inheritdoc cref="CanRead"/>
        public override bool CanRead => true;

        /// <inheritdoc cref="CanSeek"/>
        public override bool CanSeek => true;

        /// <inheritdoc cref="CanWrite"/>
        public override bool CanWrite => false;

        /// <inheritdoc cref="Length"/>
        public override long Length => _baseStream.Length + _preBufferSize;

        /// <inheritdoc cref="Position"/>
        public override long Position { get; set; }

        /// <summary>
        /// Create a new instance of <see cref="PreBufferStream"/>.
        /// </summary>
        /// <param name="baseStream">The stream to be preset with a buffer.</param>
        /// <param name="preBufferSize">The size of the zero filled buffer.</param>
        /// <param name="value">The value to fill into the buffer.</param>
        public PreBufferStream(Stream baseStream, int preBufferSize, byte value = 0)
        {
            ContractAssertions.IsNotNull(baseStream, nameof(baseStream));

            _baseStream = baseStream;
            _preBufferSize = preBufferSize;
            _value = value;
        }

        /// <inheritdoc cref="Flush"/>
        public override void Flush()
        {
            _baseStream.Flush();
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

            if (Position < _preBufferSize)
            {
                var toRead = Math.Min(cappedCount, (int)(_preBufferSize - Position));

#if NET_CORE_31
                Array.Fill<byte>(buffer, _value, offset, toRead);
#else
                for (var i = 0; i < toRead; i++)
                    buffer[offset + i] = _value;
#endif

                offset += toRead;
                Position += toRead;
                cappedCount -= toRead;
            }

            if (cappedCount > 0)
            {
                var toRead = Math.Min(cappedCount, (int)(_baseStream.Length - (Position - _preBufferSize)));

                var bkPos = _baseStream.Position;
                _baseStream.Position = Position - _preBufferSize;
                _baseStream.Read(buffer, offset, toRead);
                _baseStream.Position = bkPos;

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
                _baseStream = null;
            }

            base.Dispose(disposing);
        }
    }
}
