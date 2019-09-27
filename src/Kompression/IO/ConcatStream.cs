using System;
using System.IO;

namespace Kompression.IO
{
    public class ConcatStream : Stream
    {
        private Stream _baseStream1;
        private Stream _baseStream2;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _baseStream1.Length + _baseStream2.Length;
        public override long Position { get; set; }

        public ConcatStream(Stream baseStream1, Stream baseStream2)
        {
            _baseStream1 = baseStream1;
            _baseStream2 = baseStream2;
        }

        public override void Flush()
        {
        }

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

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

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

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
