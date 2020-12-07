using System;
using System.IO;
using Kontract;

namespace Komponent.IO.Streams
{
    public class SubStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly long _length;
        private readonly long _baseOffset;

        public override long Length => _length;
        public override bool CanRead => _baseStream.CanRead;
        public override bool CanWrite => _baseStream.CanWrite;
        public override bool CanSeek => _baseStream.CanSeek;
        public override long Position { get; set; }

        // Breaking change to Kryptography.SubStream to keeping the position
        public SubStream(Stream baseStream, long offset, long length)
        {
            ContractAssertions.IsNotNull(baseStream,nameof(baseStream));

            // Sanity Checks
            if (!baseStream.CanRead) 
                throw new ArgumentException(nameof(CanRead));
            if (!baseStream.CanSeek) 
                throw new ArgumentException(nameof(CanSeek));
            if (offset < 0) 
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + length > baseStream.Length) 
                throw new ArgumentOutOfRangeException(nameof(length));

            // Assign private members
            _baseStream = baseStream;
            _length = length;
            _baseOffset = offset;
        }

        public override void Flush() => _baseStream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => origin switch
        {
            SeekOrigin.Begin => Position = offset,
            SeekOrigin.Current => Position += offset,
            SeekOrigin.End => Position = _length + offset,
            _ => throw new ArgumentException("Origin is invalid."),
        };

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position >= _length)
                return 0;

            var restore = _baseStream.Position;

            _baseStream.Position = _baseOffset + Position;
            var read = _baseStream.Read(buffer, offset, (int)Math.Min(count, _length - Position));
            Position += read;

            _baseStream.Position = restore;

            return read;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite) throw new NotSupportedException("Write is not supported.");
            if (Position >= _length) throw new ArgumentOutOfRangeException("Stream has fixed length and Position was out of range.");
            if (_length - Position < count) throw new InvalidOperationException("Stream has fixed length and tries to write too much data.");

            var restore = _baseStream.Position;

            _baseStream.Position = _baseOffset + Position;
            _baseStream.Write(buffer, offset, count);
            Position += count;

            _baseStream.Position = restore;
        }
    }
}