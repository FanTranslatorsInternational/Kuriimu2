using Komponent.Streams;

namespace Kryptography.Encryption.Sony
{
    public abstract class KryptoStream : Stream
    {
        protected Stream BaseStream;

        public delegate void ProgressEventHandler(KryptoStream sender, long done, long total, TimeSpan elapsedTime, bool write);

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
        public override long Length => _length;

        public override long Position { get; set; }

        public KryptoStream(Stream input)
        {
            BaseStream = input;
        }

        public KryptoStream(Stream input, long offset, long length)
        {
            BaseStream = new SubStream(input, offset, length)
            {
                Position = Math.Min(Math.Max(input.Position, offset) - offset, length)
            };
            _length = length;
        }

        public KryptoStream(byte[] input)
        {
            BaseStream = new MemoryStream(input);
        }

        public KryptoStream(byte[] input, long offset, long length)
        {
            BaseStream = new SubStream(new MemoryStream(input), offset, length);
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

            var origPos = BaseStream.Position;
            BaseStream.Position = alignedPos;

            var read = 0;
            var bufOffset = offset;
            var decData = new byte[BufferSize];
            while (read < alignedCount)
            {
                var size = Math.Min(alignedCount - read, BufferSize);

                BaseStream.Position = alignedPos + read;
                var readCurrent = BaseStream.Read(decData, 0, size);

                BaseStream.Position = alignedPos + read;
                Decrypt(decData, 0, size);

                read += readCurrent;

                var decOffset = read <= size ? bytesIn : 0;
                var actualReadSize = read - decOffset;
                var decSize = size - (actualReadSize <= size ? bytesIn : 0) - (actualReadSize >= alignedCount ? alignedCount - count - (actualReadSize > size ? bytesIn : 0) : 0);
                Array.Copy(decData, decOffset, buffer, bufOffset, decSize);
                bufOffset += decSize;
            }
            Position += count;
            Position = Math.Min(Length, Position);

            BaseStream.Position = origPos;

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
                var zeroSize = (int)(Position > Length ? Math.Min(Math.Max(0, Position - Length - Math.Max(0, alignedPos + write - Length)), BufferSize) : 0);
                var encSize = (int)(alignedPos + write + BufferSize > Position ? Math.Min(alignedPos + write + BufferSize - Position, BufferSize) : 0);

                //Sanitycheck
                if (preSize + zeroSize + encSize != BufferSize)
                    throw new InvalidDataException("An error was made by the dev. Contact him with this error message. Developer: onepiecefreak3");

                //Prepare decData
                var decPos = 0;
                if (preSize > 0)
                {
                    BaseStream.Position = alignedPos + write;
                    _ = BaseStream.Read(decData, 0, preSize);

                    BaseStream.Position = alignedPos + write;
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
                }

                //Encrypt data (finally)
                BaseStream.Position = alignedPos + write;
                Encrypt(decData, 0, decData.Length);

                //Write data
                var size = Math.Min(alignedCount - write, BufferSize);
                BaseStream.Write(decData, 0, size);
                write += size;
            }
            Position += count;
            _length = Math.Max(alignedPos + alignedCount, Length);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var seeked = BaseStream.Seek(offset, origin);
            Position = seeked;
            return seeked;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                BaseStream.Dispose();
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

        private static void ValidateInput(byte[] buffer, int offset, int count)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(offset, 0);
            ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);

            if (offset + count > buffer.Length) throw new InvalidDataException("Buffer too short.");
        }

        private int GetAlignedCount(int count)
        {
            return (int)Math.Ceiling((double)count / BlockAlign) * BlockAlign;
        }

        #endregion
    }
}