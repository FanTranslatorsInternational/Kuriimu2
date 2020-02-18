using System;
using System.IO;

namespace Komponent.IO.Streams
{
    public class SubStream : Stream
    {
        private Stream _baseStream;
        private readonly long _length;
        private readonly long _baseOffset;

        public SubStream(Stream baseStream, long offset, long length)
        {
            // Sanity Checks
            if (baseStream == null) throw new ArgumentNullException(nameof(baseStream));
            if (!baseStream.CanRead) throw new ArgumentException("baseStream.CanRead is false");
            if (!baseStream.CanSeek) throw new ArgumentException("baseStream.CanSeek is false");
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + length > baseStream.Length) throw new ArgumentOutOfRangeException(nameof(length));

            // Assign private memberss
            _baseStream = baseStream;
            _length = length;
            _baseOffset = offset;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var restore = _baseStream.Position;
            _baseStream.Position = _baseOffset + offset + Position;
            var read = _baseStream.Read(buffer, offset, (int)Math.Min(count, _length - Position));
            Position += read;
            _baseStream.Position = restore;
            return read;
        }

        public override long Length => _length;
        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanSeek => true;
        public override long Position { get; set; }
        public override void Flush() => _baseStream.Flush();

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: return Position = offset;
                case SeekOrigin.Current: return Position += offset;
                case SeekOrigin.End: return Position = _length + offset;
            }
            throw new ArgumentException("origin is invalid");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}