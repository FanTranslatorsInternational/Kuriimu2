using Komponent.Contract.Enums;

namespace Komponent.IO
{
    /// <summary>
    /// Writing an arbitrary amount of bits to a given data source.
    /// </summary>
    public class BinaryBitWriter(Stream baseStream, BitOrder bitOrder, int blockSize, ByteOrder byteOrder) : IDisposable
    {
        private long _buffer;
        private byte _bufferBitPosition;

        /// <summary>
        /// Gets the current bit position.
        /// </summary>
        public long Position => baseStream.Position * 8 + _bufferBitPosition;

        /// <summary>
        /// Gets the bit length.
        /// </summary>
        public long Length => baseStream.Length * 8 + _bufferBitPosition;

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
                if (bitOrder == BitOrder.MostSignificantBitFirst)
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
            if (_bufferBitPosition >= blockSize * 8)
                WriteBuffer();

            _buffer |= ((long)value & 0x1) << _bufferBitPosition++;
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
            if (bitOrder == BitOrder.MostSignificantBitFirst)
                _buffer = ReverseBits(_buffer, blockSize * 8);

            for (var i = 0; i < blockSize; i++)
                if (byteOrder == ByteOrder.BigEndian)
                    baseStream.WriteByte((byte)(_buffer >> ((blockSize - 1 - i) * 8)));
                else
                    baseStream.WriteByte((byte)(_buffer >> (i * 8)));

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
            GC.SuppressFinalize(this);

            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Flush();
        }

        #endregion
    }
}
