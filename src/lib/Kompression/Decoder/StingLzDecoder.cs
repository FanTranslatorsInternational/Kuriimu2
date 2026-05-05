using System.Buffers.Binary;
using Kompression.Contract.Decoder;
using Kompression.Exceptions;
using Kompression.IO;

namespace Kompression.Decoder
{
    internal class StingLzDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[12];

            // Read header information
            _ = input.Read(buffer.AsSpan(0, 4));
            if (buffer[0] is not 0x4C || buffer[1] is not 0x5A || buffer[2] is not 0x37 || buffer[3] is not 0x37) // LZ77
                throw new InvalidCompressionException(nameof(StingLzDecoder));

            _ = input.Read(buffer);
            var decompressedSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            var tokenCount = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4));
            var dataOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(8));

            // Read compressed data
            var flagOffset = 0x10;

            var flags = 0;
            var flagPosition = 0;

            var circularBuffer = new CircularBuffer(0xFF);
            while (tokenCount-- > 0)
            {
                if (flagPosition == 0)
                {
                    input.Position = flagOffset++;

                    flags = input.ReadByte();
                    flagPosition = 8;
                }

                if ((flags >> --flagPosition & 1) == 0)
                {
                    // Literal
                    input.Position = dataOffset++;

                    var value = (byte)input.ReadByte();
                    output.WriteByte(value);
                    circularBuffer.WriteByte(value);
                }
                else
                {
                    // Match
                    input.Position = dataOffset;
                    dataOffset += 2;

                    var displacement = input.ReadByte();
                    var length = input.ReadByte() + 3;

                    circularBuffer.Copy(output, displacement, length);
                }
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
