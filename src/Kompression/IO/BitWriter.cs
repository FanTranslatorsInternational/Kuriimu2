using System;
using System.IO;

namespace Kompression
{
    class BitWriter : IDisposable
    {
        private readonly Stream _baseStream;
        private readonly BitOrder _bitOrder;

        private byte _buffer;
        private byte _bufferBitPosition;

        public long Position => _baseStream.Position * 8 + _bufferBitPosition;

        public long Length => _baseStream.Length * 8 + _bufferBitPosition;

        public BitWriter(Stream baseStream, BitOrder bitOrder)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _bitOrder = bitOrder;
        }

        public void WriteBit(int value)
        {
            if (_bufferBitPosition >= 8)
                WriteBuffer();

            _buffer |= (byte)((value & 0x1) << _bufferBitPosition++);
        }

        public void WriteByte(int value)
        {
            for (var i = 0; i < 8; i++)
                WriteBit((value >> (7 - i)) & 0x1);
        }

        public void WriteInt32(int value)
        {
            for (var i = 0; i < 32; i++)
                WriteBit((value >> (31 - i)) & 0x1);
        }

        public void Flush()
        {
            if (_bufferBitPosition > 0)
                WriteBuffer();
        }

        private void WriteBuffer()
        {
            if (_bitOrder == BitOrder.MSBFirst)
                _buffer = ReverseBits(_buffer);
            _baseStream.WriteByte(_buffer);
            ResetBuffer();
        }

        private void ResetBuffer()
        {
            _buffer = 0;
            _bufferBitPosition = 0;
        }

        private byte ReverseBits(byte value)
        {
            byte result = 0;

            for (var i = 0; i < 8; i++)
            {
                result <<= 1;
                result |= (byte)(value & 1);
                value >>= 1;
            }

            return result;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Flush();
                _baseStream?.Dispose();
            }
        }
    }
}
