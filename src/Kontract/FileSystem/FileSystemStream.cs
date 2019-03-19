using System;
using System.IO;

namespace Kontract.FileSystem
{
    /// <inheritdoc />
    public class FileSystemStream : Stream
    {
        private readonly Stream _baseStream;

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        public FileSystemStream(Stream input)
        {
            _baseStream = input;
        }

        public override void Flush() => _baseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

        public override void SetLength(long value) => _baseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);

        public event EventHandler<CloseStreamEventArgs> CloseStream;

        public override void Close()
        {
            CloseStream?.Invoke(this, new CloseStreamEventArgs(_baseStream));
            base.Close();
        }
    }

    /// <inheritdoc />
    public class CloseStreamEventArgs : EventArgs
    {
        public Stream BaseStream { get; }

        public CloseStreamEventArgs(Stream baseStream)
        {
            BaseStream = baseStream;
        }
    }
}
