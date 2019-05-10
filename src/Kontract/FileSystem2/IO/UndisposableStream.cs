using System;
using System.IO;

namespace Kontract.FileSystem2.IO
{
    /// <summary>
    /// A <see cref="Stream"/> which stubs the disposing and closing action.
    /// </summary>
    class UndisposableStream : Stream
    {
        private Stream _baseStream;

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;
        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        /// <summary>
        /// Creates a new instance of <see cref="UndisposableStream"/>.
        /// </summary>
        /// <param name="baseStream">The stream to embed.</param>
        public UndisposableStream(Stream baseStream)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        }

        public override void Flush()
            => _baseStream.Flush();

        public override long Seek(long offset, SeekOrigin origin)
            => _baseStream.Seek(offset, origin);

        public override void SetLength(long value)
            => _baseStream.SetLength(value);

        public override int Read(byte[] buffer, int offset, int count)
            => _baseStream.Read(buffer, offset, count);

        public override void Write(byte[] buffer, int offset, int count)
            => _baseStream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _baseStream = null;
            }
        }

        public override void Close()
        {
            _baseStream = null;
        }
    }
}
