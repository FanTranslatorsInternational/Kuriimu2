using System.Text;

namespace plugin_inti_creates.Cryptography
{
    public class IntiCreatesCipherStream : Stream
    {
        private static readonly byte[] Buffer = new byte[0x1000];
        private readonly Stream _baseStream;

        private long _position;

        private readonly ulong _initialKey;
        private ulong _currentKey;

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;
        public override long Position { get => _position; set => Seek(value, SeekOrigin.Begin); }

        public IntiCreatesCipherStream(Stream baseStream, string password)
        {
            _baseStream = baseStream;
            _currentKey = _initialKey = PrepareKeyValues(password);
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _currentKey = _initialKey;

                    CalculateKeyValues(0, offset);
                    _position = offset;
                    break;

                case SeekOrigin.Current:
                    CalculateKeyValues(_position, offset);
                    _position += offset;
                    break;

                case SeekOrigin.End:
                    throw new InvalidOperationException("Cannot set position outside the stream.");
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Read data
            var bkPos = _baseStream.Position;

            _baseStream.Position = _position;
            var readBytes = _baseStream.Read(buffer, offset, count);
            _baseStream.Position = bkPos;

            count = Math.Min(readBytes, count);

            // Decrypt data
            for (var i = _position; i < _position + count; i++)
            {
                var value = buffer[offset];
                var xorValue = _currentKey >> (int)(i & 0x1F);
                buffer[offset] = (byte)(value ^ xorValue);

                _currentKey = AdvanceKeyValue(value, _currentKey);

                offset++;
            }

            // Finalize read
            _position += count;
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var bkPos = _baseStream.Position;

            _baseStream.Position = Position;
            while (count > 0)
            {
                var length = Math.Min(count, 0x1000);
                for (var i = 0; i < length; i++)
                {
                    // Encrypt value
                    var xorValue = _currentKey >> ((int)(_position + i) & 0x1F);
                    var encValue = (byte)(buffer[offset + i] ^ xorValue);

                    // Write value
                    _baseStream.WriteByte(encValue);

                    _currentKey = AdvanceKeyValue(encValue, _currentKey);
                }

                count -= length;
                offset += length;
                _position += length;
            }

            _baseStream.Position = bkPos;
        }

        private ulong PrepareKeyValues(string password)
        {
            var key = 0xa1b34f58cad705b2;
            foreach (var b in Encoding.ASCII.GetBytes(password))
                key = AdvanceKeyValue(b, key);

            return key;
        }

        private void CalculateKeyValues(long srcPosition, long offset)
        {
            var bkPos1 = _baseStream.Position;
            _baseStream.Position = srcPosition;

            while (offset > 0)
            {
                var count = (int)Math.Min(offset, 0x1000);
                _baseStream.Read(Buffer, 0, count);

                for (var i = 0; i < count; i++)
                    _currentKey = AdvanceKeyValue(Buffer[i], _currentKey);

                offset -= count;
            }

            _baseStream.Position = bkPos1;
        }

        private ulong AdvanceKeyValue(byte value, ulong key)
        {
            return (key + value) * 0x8D;
        }
    }
}
