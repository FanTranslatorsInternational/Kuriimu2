using System;
using System.IO;

namespace Kompression.IO
{
    /// <summary>
    /// Writing an arbitrary amount of bits to a given data source.
    /// </summary>
    public class BitWriter : IDisposable
    {
        private readonly Stream _baseStream;
        private readonly ByteOrder _byteOrder;
        private readonly BitOrder _bitOrder;
        private readonly int _blockSize;

        private long _buffer;
        private byte _bufferBitPosition;

        /// <summary>
        /// Gets the current bit position.
        /// </summary>
        public long Position => _baseStream.Position * 8 + _bufferBitPosition;

        /// <summary>
        /// Gets the bit length.
        /// </summary>
        public long Length => _baseStream.Length * 8 + _bufferBitPosition;

        /// <summary>
        /// Creates a new instance of <see cref="BitWriter"/>.
        /// </summary>
        /// <param name="baseStream">The base data source to write to.</param>
        /// <param name="bitOrder">The order in which to write the bits.</param>
        /// <param name="blockSize">The size of the bit buffer in bytes.</param>
        /// <param name="byteOrder">The order in which to write the bytes for the buffer.</param>
        public BitWriter(Stream baseStream, BitOrder bitOrder, int blockSize, ByteOrder byteOrder)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _bitOrder = bitOrder;
            _blockSize = blockSize;
            _byteOrder = byteOrder;
        }

        /// <summary>
        /// Writes 8 bits to the data source.
        /// </summary>
        public void WriteByte(int value) => WriteBits(value, 8);

        /// <summary>
        /// Writes 16 bits to the data source.
        /// </summary>
        public void WriteInt16(int value) => WriteBits(value, 16);

        /// <summary>
        /// Writes 32 bits to the data source.
        /// </summary>
        public void WriteInt32(int value) => WriteBits(value, 32);

        /// <summary>
        /// Write an arbitrary number of bits.
        /// </summary>
        /// <param name="value">The value to write bits from.</param>
        /// <param name="count">The number of bits to write.</param>
        /// <remarks>Refer to source code for the used design pattern.</remarks>
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
             * Assuming MsbFirst, we would now write the values
             * 0b101 and 0b10001
             *
             * Assuming LsbFirst, we would now write the values
             * 0b10001 and 0b101
             *
             * Even though the values generate a different final byte,
             * the order of bits in the values is still intact
             *
             */

            for (var i = 0; i < count; i++)
            {
                if (_bitOrder == BitOrder.MsbFirst)
                {
                    WriteBit(value >> (count - 1 - i));
                }
                else
                {
                    WriteBit(value >> i);
                }
            }
        }

        /// <summary>
        /// Write a single bit.
        /// </summary>
        public void WriteBit(int value)
        {
            if (_bufferBitPosition >= _blockSize * 8)
                WriteBuffer();

            _buffer |= (long)((value & 0x1) << _bufferBitPosition++);
        }

        /// <summary>
        /// Flushes all internal buffers into the data source no matter their state.
        /// </summary>
        public void Flush()
        {
            if (_bufferBitPosition > 0)
                WriteBuffer();
        }

        /// <summary>
        /// Writes the buffer to the data source.
        /// </summary>
        private void WriteBuffer()
        {
            if (_bitOrder == BitOrder.MsbFirst)
                _buffer = ReverseBits(_buffer, _blockSize * 8);

            for (var i = 0; i < _blockSize; i++)
                if (_byteOrder == ByteOrder.BigEndian)
                    _baseStream.WriteByte((byte)(_buffer >> ((_blockSize - 1 - i) * 8)));
                else
                    _baseStream.WriteByte((byte)(_buffer >> (i * 8)));

            ResetBuffer();
        }

        /// <summary>
        /// Resets the buffer to its initial state.
        /// </summary>
        private void ResetBuffer()
        {
            _buffer = 0;
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
                Flush();
                _baseStream?.Dispose();
            }
        }

        #endregion
    }
}
