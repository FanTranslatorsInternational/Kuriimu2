namespace Kompression.Encoder.LempelZiv.InputManipulation.Streams
{
    internal class PreBufferStream : Stream
    {
        //private readonly int _preBufferSize;
        //private readonly byte _value;
        private readonly byte[] _data;
        private readonly Stream _baseStream;

        /// <inheritdoc cref="CanRead"/>
        public override bool CanRead => true;

        /// <inheritdoc cref="CanSeek"/>
        public override bool CanSeek => true;

        /// <inheritdoc cref="CanWrite"/>
        public override bool CanWrite => false;

        /// <inheritdoc cref="Length"/>
        public override long Length => _baseStream.Length + _data.Length;

        /// <inheritdoc cref="Position"/>
        public override long Position { get; set; }

        /// <summary>
        /// Create a new instance of <see cref="PreBufferStream"/>.
        /// </summary>
        /// <param name="baseStream">The stream to be preset with a buffer.</param>
        /// <param name="preBufferSize">The size of the pre-filled buffer.</param>
        /// <param name="value">The value to fill into the buffer.</param>
        public PreBufferStream(Stream baseStream, int preBufferSize, byte value = 0)
        {
            _baseStream = baseStream;
            _data = new byte[preBufferSize];

            Array.Fill(_data, value, 0, preBufferSize);
        }

        /// <summary>
        /// Create a new instance of <see cref="PreBufferStream"/>.
        /// </summary>
        /// <param name="baseStream">The stream to be preset with a buffer.</param>
        /// <param name="data">The pre-filled buffer.</param>
        public PreBufferStream(Stream baseStream, byte[] data)
        {
            _baseStream = baseStream;
            _data = data;
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

            if (Position < _data.Length)
            {
                var toRead = Math.Min(cappedCount, (int)(_data.Length - Position));

                Array.Copy(_data, Position, buffer, offset, toRead);

                offset += toRead;
                Position += toRead;
                cappedCount -= toRead;
            }

            if (cappedCount > 0)
            {
                var toRead = Math.Min(cappedCount, (int)(_baseStream.Length - (Position - _data.Length)));

                var bkPos = _baseStream.Position;
                _baseStream.Position = Position - _data.Length;
                _ = _baseStream.Read(buffer, offset, toRead);
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
    }
}
