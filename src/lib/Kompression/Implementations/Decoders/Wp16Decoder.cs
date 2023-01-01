using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Kompression.IO;
using Kontract.Kompression.Interfaces.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class Wp16Decoder : IDecoder
    {
        private const int PreBufferSize_ = 0xFFE;

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            if (Encoding.ASCII.GetString(buffer) != "Wp16")
                throw new InvalidOperationException("Not Wp16 compressed.");

            input.Read(buffer, 0, 4);
            var decompressedSize = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(0));

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
                    input.Read(buffer, 0, 4);
                    flags = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(0));
                    flagPosition = 0;
                }

                if (((flags >> flagPosition++) & 0x1) == 1)
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

                    var displacement = (byte2 << 3) | (byte1 >> 5);
                    var length = (byte1 & 0x1F) + 2;

                    circularBuffer.Copy(output, displacement * 2, length * 2);
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
