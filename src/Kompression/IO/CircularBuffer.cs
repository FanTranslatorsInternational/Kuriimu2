using System;
using System.IO;

namespace Kompression.IO
{
    /// <summary>
    /// A buffer to read and write data in a circular manner.
    /// </summary>
    public class CircularBuffer : IDisposable
    {
        private byte[] _buffer;

        /// <summary>
        /// Gets the length of the buffer.
        /// </summary>
        public int Length => _buffer.Length;

        /// <summary>
        /// Gets or sets the position of the data this buffer represents.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Gets the position in the buffer.
        /// </summary>
        public int RelativePosition => Position % Length;

        /// <summary>
        /// Creates a new instance of <see cref="CircularBuffer"/>.
        /// </summary>
        /// <param name="bufferSize">The size of the buffer.</param>
        public CircularBuffer(int bufferSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            _buffer = new byte[bufferSize];
        }

        /// <summary>
        /// Reads a byte from the buffer and advances the position.
        /// </summary>
        /// <returns>The read value.</returns>
        public int ReadByte() => _buffer[Position++ % Length];

        /// <summary>
        /// Reads a sequence of bytes.
        /// </summary>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The offset in the buffer to write to.</param>
        /// <param name="length">The length of the sequence to read.</param>
        public void Read(byte[] buffer, int offset, int length)
        {
            while (length > 0)
            {
                var toRead = Math.Min(Length - RelativePosition, length);
                Array.Copy(_buffer, RelativePosition, buffer, offset, toRead);

                Position += toRead;
                offset += toRead;
                length -= toRead;
            }
        }

        /// <summary>
        /// Writes a byte into the buffer and advances the position.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteByte(byte value) => _buffer[Position++ % Length] = value;

        /// <summary>
        /// Writes a sequence of bytes.
        /// </summary>
        /// <param name="buffer">The buffer to write from.</param>
        /// <param name="offset">The offset in the buffer to write from.</param>
        /// <param name="length">The length of the sequence to write.</param>
        public void Write(byte[] buffer, int offset, int length)
        {
            while (length > 0)
            {
                var toWrite = Math.Min(Length - RelativePosition, length);
                Array.Copy(buffer, offset, _buffer, RelativePosition, toWrite);

                Position += toWrite;
                offset += toWrite;
                length -= toWrite;
            }
        }

        /// <inheritdoc cref="Dispose"/>
        public void Dispose()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _buffer = null;
        }

        /// <summary>
        /// Copies a portion of the buffer to a designated output.
        /// </summary>
        /// <param name="output">The output to copy to.</param>
        /// <param name="displacement">The displacement from the current position of the buffer to copy from.</param>
        /// <param name="length">The length of the data to copy.</param>
        /// <remarks>For main use in Lempel-Ziv compressions.</remarks>
        public void Copy(Stream output, int displacement, int length)
        {
            var displacedBufferPosition = Position - displacement;
            var endBufferPosition = Position;

            var absoluteDisplacement = Math.Abs(displacement);
            var buffer = new byte[absoluteDisplacement];
            for (int i = 0; i < length; i += absoluteDisplacement)
            {
                var toCopy = Math.Min(absoluteDisplacement, length - i);

                Position = displacedBufferPosition;
                Read(buffer, 0, toCopy);

                output.Write(buffer, 0, toCopy);

                Position = endBufferPosition;
                Write(buffer, 0, toCopy);

                displacedBufferPosition += toCopy;
                endBufferPosition += toCopy;
            }
        }
    }
}
