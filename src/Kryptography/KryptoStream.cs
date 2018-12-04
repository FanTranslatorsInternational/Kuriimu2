using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Kryptography
{
    public abstract class KryptoStream : Stream
    {
        protected Stream _baseStream;

        public delegate void ProgressEventHandler(KryptoStream sender, long done, long total, TimeSpan elapsedTime, bool write);
        public event ProgressEventHandler Progress;

        public abstract int BlockSize { get; }
        public abstract int BlockSizeBytes { get; }

        protected abstract int BlockAlign { get; }
        protected abstract int SectorAlign { get; }
        protected virtual int BufferSize { get; } = 0x10000;

        public abstract List<byte[]> Keys { get; protected set; }
        public abstract int KeySize { get; }

        public abstract byte[] IV { get; protected set; }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;

        private long _length;
        public override long Length { get => _length; }

        public override long Position { get; set; }

        public KryptoStream(Stream input)
        {
            _baseStream = input;
        }

        public KryptoStream(Stream input, long offset, long length)
        {
            _baseStream = new SubStream(input, offset, length);
            _length = length;
        }

        public KryptoStream(byte[] input)
        {
            _baseStream = new MemoryStream(input);
        }

        public KryptoStream(byte[] input, long offset, long length)
        {
            _baseStream = new SubStream(input, offset, length);
            _length = length;
        }

        protected abstract void Decrypt(byte[] buffer, int offset, int count);

        protected abstract void Encrypt(byte[] buffer, int offset, int count);

        #region Overrides
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateRead(buffer, offset, count);
            if (Position >= Length) return 0;

            var alignedPos = Position / SectorAlign * SectorAlign;
            var bytesIn = (int)(Position % SectorAlign);
            var alignedCount = GetAlignedCount((int)Math.Min(Position - alignedPos + count, Length - alignedPos));
            if (alignedCount == 0) return 0;

            var origPos = _baseStream.Position;
            _baseStream.Position = alignedPos;

            var read = 0;
            var decData = new byte[BufferSize];
            while (read < alignedCount)
            {
                Array.Clear(decData, 0, decData.Length);

                _baseStream.Position = alignedPos + read;
                var preRead = read;

                var size = Math.Min(alignedCount - read, BufferSize);
                read += _baseStream.Read(decData, 0, size);

                _baseStream.Position = alignedPos + preRead;
                Decrypt(decData, 0, size);

                var copyOffset = (read <= size) ? bytesIn : 0;
                read -= copyOffset;
                var copySize = size - (read <= size ? bytesIn : 0) - (read >= alignedCount ? alignedCount - count - (read <= size ? bytesIn : 0) : 0);
                Array.Copy(decData, copyOffset, buffer, offset + preRead, copySize);
            }
            Position += count;
            Position = Math.Min(Length, Position);

            _baseStream.Position = origPos;

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateWrite(buffer, offset, count);
            if (count == 0) return;

            var alignedPos = Math.Min(Length, Position) / SectorAlign * SectorAlign;
            //var bytesIn = (int)(Position % SectorAlign);
            var alignedCount = GetAlignedCount((int)(Position - alignedPos + count));
            if (alignedCount == 0) return;

            var write = 0;
            var encPos = 0;
            var decData = new byte[BufferSize];
            while (write < alignedCount)
            {
                var preSize = Math.Min(Math.Max(0, (int)(Length - alignedPos - write)), BufferSize);
                var zeroSize = (int)((Position > Length) ? Math.Min(Math.Max(0, Position - Length - Math.Max(0, alignedPos + write - Length)), BufferSize) : 0);
                var encSize = (int)(alignedPos + write + BufferSize > Position ? Math.Min(alignedPos + write + BufferSize - Position, BufferSize) : 0);

                //Sanitycheck
                if (preSize + zeroSize + encSize != BufferSize)
                    throw new InvalidDataException("An error was made by the dev. Contact him with this error message. Developer: onepiecefreak3");

                //Prepare decData
                var decPos = 0;
                if (preSize > 0)
                {
                    _baseStream.Position = alignedPos + write;
                    _baseStream.Read(decData, 0, preSize);

                    _baseStream.Position = alignedPos + write;
                    Decrypt(decData, 0, preSize);

                    decPos += preSize;
                }
                if (zeroSize > 0)
                {
                    Array.Clear(decData, decPos, zeroSize);
                    decPos += zeroSize;
                }
                if (encSize > 0)
                {
                    var decSize = encPos + encSize > count ? count - encPos : encSize;
                    Array.Copy(buffer, offset + encPos, decData, decPos, decSize);
                    encPos += decSize;
                    decPos += decSize;
                }

                //Encrypt data (finally)
                _baseStream.Position = alignedPos + write;
                Encrypt(decData, 0, decData.Length);

                //Write data
                var size = Math.Min(alignedCount - write, BufferSize);
                _baseStream.Write(decData, 0, size);
                write += size;
            }
            Position += count;
            _length = Math.Max(alignedPos + alignedCount, Length);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var seeked = _baseStream.Seek(offset, origin);
            Position = seeked;
            return seeked;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _baseStream.Dispose();
            }
        }
        #endregion

        #region Private Methods
        private void ValidateRead(byte[] buffer, int offset, int count)
        {
            if (!CanRead) throw new NotSupportedException("Reading is not supported.");

            ValidateInput(buffer, offset, count);
        }

        private void ValidateWrite(byte[] buffer, int offset, int count)
        {
            if (!CanWrite) throw new NotSupportedException("Write is not supported");

            ValidateInput(buffer, offset, count);
        }

        private void ValidateInput(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Offset or count can't be negative.");
            if (offset + count > buffer.Length) throw new InvalidDataException("Buffer too short.");
        }

        private int GetAlignedCount(int count)
        {
            return (int)Math.Ceiling((double)count / BlockAlign) * BlockAlign;
        }
        #endregion
    }
}