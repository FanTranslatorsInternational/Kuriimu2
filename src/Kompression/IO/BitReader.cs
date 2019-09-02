using System;
using System.IO;

namespace Kompression.IO
{
    public class BitReader : IDisposable
    {
        private readonly Stream _baseStream;
        private readonly BitOrder _bitOrder;
        private readonly ByteOrder _byteOrder;
        private readonly int _blockSize;

        private long _buffer;
        private byte _bufferBitPosition;

        public long Position
        {
            get => (_baseStream.Position - _blockSize) / _blockSize * _blockSize * 8 + _bufferBitPosition;
            set => SetBitPosition(value);
        }

        public long Length => _baseStream.Length * 8;

        public BitReader(Stream baseStream, BitOrder bitOrder, int blockSize, ByteOrder byteOrder)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _bitOrder = bitOrder;
            _blockSize = blockSize;
            _byteOrder = byteOrder;

            if (_baseStream.Length % _blockSize != 0)
                throw new InvalidOperationException("Stream length must be dividable by block size.");

            RefillBuffer();
        }

        public int ReadByte() => ReadBits<int>(8);

        public int ReadInt16() => ReadBits<int>(16);

        public int ReadInt32() => ReadBits<int>(32);

        public object ReadBits(int count)
        {
            /*
             * This method is designed with direct mapping in mind.
             *
             * Example:
             * You have a byte 0x83, which in bits would be
             * 0b1000 0011
             *
             * Assume we read 3 bits and 5 bits afterwards
             *
             * Assuming MSBFirst, we would now read the values
             * 0b100 and 0b00011
             *
             * Assuming LSBFirst, we would now read the values
             * 0b011 and 0b10000
             *
             * Even though the values themselves changed, the order of bits is still intact
             *
             * Combine 0b100 and 0b00011 and you get the original byte
             * Combine 0b10000 and 0b011 and you also get the original byte
             *
             */

            long result = 0;
            for (var i = 0; i < count; i++)
            {
                if (_bitOrder == BitOrder.MSBFirst)
                {
                    result <<= 1;
                    result |= (byte)ReadBit();
                }
                else
                {
                    result |= (long)(ReadBit() << i);
                }
            }

            return result;
        }

        public T ReadBits<T>(int count)
        {
            if (typeof(T) != typeof(bool) &&
                typeof(T) != typeof(sbyte) && typeof(T) != typeof(byte) &&
                typeof(T) != typeof(short) && typeof(T) != typeof(ushort) &&
                typeof(T) != typeof(int) && typeof(T) != typeof(uint) &&
                typeof(T) != typeof(long) && typeof(T) != typeof(ulong))
                throw new InvalidOperationException($"Unsupported type {typeof(T)}.");

            var value = ReadBits(count);

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public int ReadBit()
        {
            if (_bufferBitPosition >= _blockSize * 8)
                RefillBuffer();

            return (int)((_buffer >> _bufferBitPosition++) & 0x1);
        }

        public T SeekBits<T>(int count)
        {
            var originalPosition = Position;

            var result = ReadBits<T>(count);
            SetBitPosition(originalPosition);

            return result;
        }

        private void SetBitPosition(long bitPosition)
        {
            _baseStream.Position = bitPosition / (_blockSize * 8);
            RefillBuffer();
            _bufferBitPosition = (byte)(bitPosition % (_blockSize * 8));
        }

        private void RefillBuffer()
        {
            _buffer = 0;

            // Read buffer with blockSize bytes
            for (var i = 0; i < _blockSize; i++)
                if (_byteOrder == ByteOrder.BigEndian)
                    _buffer = (_buffer << 8) | (byte)_baseStream.ReadByte();
                else
                    _buffer = _buffer | (long)((byte)_baseStream.ReadByte() << (i * 8));

            if (_bitOrder == BitOrder.MSBFirst)
                _buffer = ReverseBits(_buffer, _blockSize * 8);

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
                _baseStream?.Dispose();
            }
        }
    }
}
