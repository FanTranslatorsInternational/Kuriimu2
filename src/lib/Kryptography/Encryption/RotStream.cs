using System.Numerics;

namespace Kryptography.Encryption
{
    public sealed class RotStream(Stream input, byte key) : Stream
    {
        public override bool CanRead => input.CanRead;

        public override bool CanSeek => input.CanSeek;

        public override bool CanWrite => input.CanWrite;

        public override long Length => input.Length;

        public override long Position { get; set; }

        private static void RotData(byte[] buffer, int offset, int count, byte rotBy)
        {
            var simdLength = Vector<byte>.Count;
            var rotBuffer = new byte[simdLength];
            for (int i = 0; i < simdLength; i++)
                rotBuffer[i] = rotBy;
            var vr = new Vector<byte>(rotBuffer);

            int j;
            for (j = 0; j <= count - simdLength; j += simdLength)
            {
                var va = new Vector<byte>(buffer, j + offset);
                (va + vr).CopyTo(buffer, j + offset);
            }

            for (; j < count; ++j)
                buffer[offset + j] += rotBy;
        }

        public override void Flush() => input.Flush();

        public override void SetLength(long value)
        {
            if (!CanWrite || !CanSeek)
                throw new NotSupportedException("Can't set length of stream.");
            if (value < 0)
                throw new IOException("Length can't be smaller than 0.");

            if (value > Length)
            {
                var bkPosThis = Position;
                var bkPosBase = input.Position;

                var startPos = Math.Max(input.Length, Length);
                var newDataLength = value - startPos;
                var written = 0;
                var newData = new byte[0x10000];
                while (written < newDataLength)
                {
                    Position = startPos;
                    var toWrite = (int)Math.Min(0x10000, newDataLength - written);
                    Write(newData, 0, toWrite);
                    written += toWrite;
                    startPos += toWrite;
                }

                input.Position = bkPosBase;
                Position = bkPosThis;
            }
            else
                input.SetLength(value);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException("Can't seek stream.");

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

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Can't read from stream.");

            var bkPos = input.Position;
            input.Position = Position;
            _ = input.Read(buffer, offset, count);
            input.Position = bkPos;

            RotData(buffer, offset, count, (byte)(0x100 - key));

            Position += count;
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Can't write to stream.");

            RotData(buffer, offset, count, key);

            var bkPos = input.Position;
            input.Position = Position;
            input.Write(buffer, offset, count);
            input.Position = bkPos;

            Position += count;
        }
    }
}