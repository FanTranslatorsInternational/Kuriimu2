using System.Buffers.Binary;
using System.IO;
using Kompression.Exceptions;
using Kompression.IO;
using Kontract.Kompression.Interfaces.Configuration;

namespace Kompression.Implementations.Decoders
{
    class StingLzDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];

            // Read header information
            input.Read(buffer, 0, 4);
            var magic = BinaryPrimitives.ReadUInt32BigEndian(buffer);
            if (magic != 0x4C5A3737)   // LZ77
                throw new InvalidCompressionException(nameof(StingLzDecoder));

            input.Read(buffer, 0, 4);
            var decompressedSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            input.Read(buffer, 0, 4);
            var tokenCount = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            input.Read(buffer, 0, 4);
            var dataOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer);

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

                if (((flags >> --flagPosition) & 1) == 0)
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
            // Nothing to dispose
        }
    }
}
