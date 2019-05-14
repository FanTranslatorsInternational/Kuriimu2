using System;
using System.IO;

namespace Kontract.FileSystem.IO
{
    /// <summary>
    /// A <see cref="Stream"/> which reports a closing action.
    /// </summary>
    class ReportCloseStream : Stream
    {
        private readonly Stream _baseStream;

        /// <summary>
        /// An event invoked on closing or disposing the <see cref="Stream"/>.
        /// </summary>
        public event EventHandler Closed;

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
        /// Creates a new instance of <see cref="ReportCloseStream"/>.
        /// </summary>
        /// <param name="baseStream">The stream to embed.</param>
        public ReportCloseStream(Stream baseStream)
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
                Closed?.Invoke(this, new EventArgs());
                _baseStream.Dispose();
            }

            base.Dispose(disposing);
        }

        public override void Close()
        {
            Closed?.Invoke(this, new EventArgs());
            _baseStream.Close();
            base.Close();
        }
    }
}
