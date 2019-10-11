using System;
using System.IO;

namespace Kompression.IO
{
    /// <summary>
    /// Reading an arbitrary amount of bits from a given data source.
    /// </summary>
    public class BitReader : IDisposable
    {
        private Stream _baseStream;

        private readonly BitOrder _bitOrder;
        private readonly ByteOrder _byteOrder;
        private readonly int _blockSize;

        private long _buffer;
        private byte _bufferBitPosition;

        /// <summary>
        /// Gets or sets the current bit position.
        /// </summary>
        public long Position
        {
            get => (_baseStream.Position - _blockSize) / _blockSize * _blockSize * 8 + _bufferBitPosition;
            set => SetBitPosition(value);
        }

        /// <summary>
        /// Gets the bit length.
        /// </summary>
        public long Length => _baseStream.Length * 8;

        /// <summary>
        /// Creates a new instance of <see cref="BitReader"/>.
        /// </summary>
        /// <param name="baseStream">The base data source to read bits from.</param>
        /// <param name="bitOrder">The order in which to read the bits.</param>
        /// <param name="blockSize">The size of the bit buffer in bytes.</param>
        /// <param name="byteOrder">The order in which to read the bytes for the buffer.</param>
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

        /// <summary>
        /// Reads 8 bits as an <see cref="int"/>.
        /// </summary>
        /// <returns>The read value.</returns>
        public int ReadByte() => ReadBits<int>(8);

        /// <summary>
        /// Reads 16 bits as an <see cref="int"/>.
        /// </summary>
        /// <returns>The read value.</returns>
        public int ReadInt16() => ReadBits<int>(16);

        /// <summary>
        /// Reads 32 bits as an <see cref="int"/>.
        /// </summary>
        /// <returns>The read value.</returns>
        public int ReadInt32() => ReadBits<int>(32);

        /// <summary>
        /// Read an arbitrary number of bits.
        /// </summary>
        /// <param name="count">The number of bits to read.</param>
        /// <returns>The read value.</returns>
        /// <remarks>Refer to source code for the used design pattern.</remarks>
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
             * Assuming MsbFirst, we would now read the values
             * 0b100 and 0b00011
             *
             * Assuming LsbFirst, we would now read the values
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
                if (_bitOrder == BitOrder.MsbFirst)
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

        /// <summary>
        /// Read an arbitrary number of bits.
        /// </summary>
        /// <typeparam name="T">The return type of the read value.</typeparam>
        /// <param name="count">The number of bits to read.</param>
        /// <returns>The read value as <see cref="T"/>.</returns>
        /// <remarks>Refer to source code for the used design pattern.</remarks>
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

        /// <summary>
        /// Read a single bit as an <see cref="int"/>.
        /// </summary>
        /// <returns>The read value.</returns>
        public int ReadBit()
        {
            if (_bufferBitPosition >= _blockSize * 8)
                RefillBuffer();

            return (int)((_buffer >> _bufferBitPosition++) & 0x1);
        }

        /// <summary>
        /// Seeks an arbitrary number of bits without changing the bit position.
        /// </summary>
        /// <typeparam name="T">The return type of the seek value.</typeparam>
        /// <param name="count">The number of bits to read.</param>
        /// <returns>The seek value as <see cref="T"/>.</returns>
        public T SeekBits<T>(int count)
        {
            var originalPosition = Position;

            var result = ReadBits<T>(count);
            SetBitPosition(originalPosition);

            return result;
        }

        /// <summary>
        /// Sets the bit position and updates the state of the reader accordingly.
        /// </summary>
        /// <param name="bitPosition">The new absolute bit position.</param>
        private void SetBitPosition(long bitPosition)
        {
            _baseStream.Position = bitPosition / (_blockSize * 8);
            RefillBuffer();
            _bufferBitPosition = (byte)(bitPosition % (_blockSize * 8));
        }

        /// <summary>
        /// Refill the bit buffer.
        /// </summary>
        private void RefillBuffer()
        {
            _buffer = 0;

            // Read buffer with blockSize bytes
            for (var i = 0; i < _blockSize; i++)
                if (_byteOrder == ByteOrder.BigEndian)
                    _buffer = (_buffer << 8) | (byte)_baseStream.ReadByte();
                else
                    _buffer = _buffer | (long)((byte)_baseStream.ReadByte() << (i * 8));

            if (_bitOrder == BitOrder.MsbFirst)
                _buffer = ReverseBits(_buffer, _blockSize * 8);

            _bufferBitPosition = 0;
        }

        /// <summary>
        /// Reverses the bits of a given value.
        /// </summary>
        /// <param name="value">The value bits to reverse.</param>
        /// <param name="bitCount">The number of bits to reverse.</param>
        /// <returns>The bit reversed value.</returns>
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

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _baseStream = null;
            }
        }

        #endregion
    }
}
