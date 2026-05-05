using System.Buffers.Binary;
using Kompression.Contract.Decoder;
using Kompression.Exceptions;
using Kompression.IO;

namespace Kompression.Decoder
{
    public class Wp16Decoder : IDecoder
    {
        private const int PreBufferSize_ = 0xFFE;

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];

            _ = input.Read(buffer);
            if (buffer[0] is not 0x57 || buffer[1] is not 0x70 || buffer[2] is not 0x31 || buffer[3] is not 0x36)
                throw new InvalidCompressionException("Wp16");

            _ = input.Read(buffer);
            int decompressedSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);

            var circularBuffer = new CircularBuffer(0xFFE)
            {
                Position = PreBufferSize_
            };

            long flags = 0;
            var flagPosition = 32;
            while (output.Length < decompressedSize)
            {
                if (flagPosition == 32)
                {
                    _ = input.Read(buffer);
                    flags = BinaryPrimitives.ReadInt32LittleEndian(buffer);
                    flagPosition = 0;
                }

                if ((flags >> flagPosition++ & 0x1) == 1)
                {
                    // Copy 2 bytes from input

                    var value = (byte)input.ReadByte();
                    output.WriteByte(value);
                    circularBuffer.WriteByte(value);

                    value = (byte)input.ReadByte();
                    output.WriteByte(value);
                    circularBuffer.WriteByte(value);
                }
                else
                {
                    // Read the Lz match
                    // min displacement 2, max displacement 0xFFE
                    // min length 2, max length 0x42

                    var byte1 = input.ReadByte();
                    var byte2 = input.ReadByte();

                    var displacement = byte2 << 3 | byte1 >> 5;
                    var length = (byte1 & 0x1F) + 2;

                    circularBuffer.Copy(output, displacement * 2, length * 2);
                }
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
