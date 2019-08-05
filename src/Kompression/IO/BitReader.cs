using System;
using System.IO;

namespace Kompression
{
    class BitReader:IDisposable
    {
        private readonly Stream _baseStream;
        private readonly BitOrder _bitOrder;

        private byte _buffer;
        private byte _bufferBitPosition;

        public long Position
        {
            get => (_baseStream.Position - 1) * 8 + _bufferBitPosition;
            set => SetBitPosition(value);
        }

        public long Length => _baseStream.Length * 8;

        public BitReader(Stream baseStream, BitOrder bitOrder)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _bitOrder = bitOrder;
            RefillBuffer();
        }

        public int ReadBit()
        {
            if (_bufferBitPosition >= 8)
                RefillBuffer();

            return (_buffer >> _bufferBitPosition++) & 0x1;
        }

        public int ReadByte()
        {
            var result = 0;
            for (var i = 0; i < 8; i++)
            {
                result <<= 1;
                result |= ReadBit();
            }

            return result;
        }

        public int ReadInt32()
        {
            var result = 0;
            for (var i = 0; i < 32; i++)
            {
                result <<= 1;
                result |= ReadBit();
            }

            return result;
        }

        private void SetBitPosition(long bitPosition)
        {
            _baseStream.Position = bitPosition / 8;
            RefillBuffer();
            _bufferBitPosition = (byte)(bitPosition % 8);
        }

        private void RefillBuffer()
        {
            _buffer = (byte)_baseStream.ReadByte();
            if (_bitOrder == BitOrder.MSBFirst)
                _buffer = ReverseBits(_buffer);
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
                _baseStream?.Dispose();
            }
        }
    }
}
