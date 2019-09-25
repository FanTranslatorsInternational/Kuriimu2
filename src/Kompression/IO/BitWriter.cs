using System;
using System.IO;

namespace Kompression.IO
{
    class BitWriter : IDisposable
    {
        private readonly Stream _baseStream;
        private readonly ByteOrder _byteOrder;
        private readonly BitOrder _bitOrder;
        private readonly int _blockSize;

        private long _buffer;
        private byte _bufferBitPosition;

        public long Position => _baseStream.Position * 8 + _bufferBitPosition;

        public long Length => _baseStream.Length * 8 + _bufferBitPosition;

        public BitWriter(Stream baseStream, BitOrder bitOrder, int blockSize, ByteOrder byteOrder)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _bitOrder = bitOrder;
            _blockSize = blockSize;
            _byteOrder = byteOrder;
        }

        public void WriteByte(int value) => WriteBits(value, 8);

        public void WriteInt16(int value) => WriteBits(value, 16);

        public void WriteInt32(int value) => WriteBits(value, 32);

        public void WriteBits(int value, int count)
        {
            /*
             * This method is designed with direct mapping in mind.
             *
             * Example:
             * You have two values 0x5 and 0x9, which in bits would be
             * 0b101 and 0b10001
             *
             * Assume we write them as 3 and 5 bits
             *
             * Assuming MSBFirst, we would now write the values
             * 0b101 and 0b10001
             *
             * Assuming LSBFirst, we would now write the values
             * 0b10001 and 0b101
             *
             * Even though the values generate a different final byte,
             * the order of bits in the values is still intact
             *
             */

            for (var i = 0; i < count; i++)
            {
                if (_bitOrder == BitOrder.MSBFirst)
                {
                    WriteBit(value >> (count - 1 - i));
                }
                else
                {
                    WriteBit(value >> i);
                }
            }
        }

        public void WriteBit(int value)
        {
            if (_bufferBitPosition >= _blockSize * 8)
                WriteBuffer();

            _buffer |= (long)((value & 0x1) << _bufferBitPosition++);
        }

        public void Flush()
        {
            if (_bufferBitPosition > 0)
                WriteBuffer();
        }

        private void WriteBuffer()
        {
            if (_bitOrder == BitOrder.MSBFirst)
                _buffer = ReverseBits(_buffer, _blockSize * 8);

            for (var i = 0; i < _blockSize; i++)
                if (_byteOrder == ByteOrder.BigEndian)
                    _baseStream.WriteByte((byte)(_buffer >> ((_blockSize - 1 - i) * 8)));
                else
                    _baseStream.WriteByte((byte)(_buffer >> (i * 8)));

            ResetBuffer();
        }

        private void ResetBuffer()
        {
            _buffer = 0;
            _bufferBitPosition = 0;
        }

        private static long ReverseBits(long value, int bitCount)
        {
            long result = 0;

            for (var i = 0; i < bitCount; i++)
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
