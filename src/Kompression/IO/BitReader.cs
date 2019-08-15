using System;
using System.IO;

namespace Kompression.IO
{
    class BitReader : IDisposable
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

        public int ReadBit()
        {
            if (_bufferBitPosition >= _blockSize * 8)
                RefillBuffer();

            return (int)((_buffer >> _bufferBitPosition++) & 0x1);
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

        public object ReadBits(int count)
        {
            long result = 0;
            for (var i = 0; i < count; i++)
            {
                result <<= 1;
                result |= (byte)ReadBit();
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

        private void SetBitPosition(long bitPosition)
        {
            _baseStream.Position = bitPosition / (_blockSize * 8);
            RefillBuffer();
            _bufferBitPosition = (byte)(bitPosition % (_blockSize * 8));
        }

        private void RefillBuffer()
        {
            _buffer = 0;
            for (var i = 0; i < _blockSize; i++)
                if (_byteOrder == ByteOrder.BigEndian)
                    _buffer = (_buffer << 8) | (byte)_baseStream.ReadByte();
                else
                    _buffer = _buffer | (long)((byte)_baseStream.ReadByte() << (i * 8));

            if (_bitOrder == BitOrder.MSBFirst)
                _buffer = ReverseBits(_buffer);

            _bufferBitPosition = 0;
        }

        private long ReverseBits(long value)
        {
            long result = 0;

            for (var i = 0; i < _blockSize * 8; i++)
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
