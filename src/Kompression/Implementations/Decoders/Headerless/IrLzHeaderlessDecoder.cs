using System.IO;
using Kompression.Exceptions;
using Kompression.IO;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders.Headerless
{
    // Basically Lz10, but byte1 and 2 are swapped, and the length increment is 2 instead of 3
    public class IrLzHeaderlessDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var circularBuffer = new CircularBuffer(0x1000);

            int flags = 0, mask = 0x80;
            while (input.Position < input.Length)
            {
                if (mask == 0x80)
                {
                    flags = input.ReadByte();
                    if (flags < 0)
                        throw new StreamTooShortException();

                    mask = 0x01;
                }
                else
                {
                    mask <<= 1;
                }

                if ((flags & mask) > 0)
                    HandleCompressedBlock(input, output, circularBuffer);
                else
                    HandleUncompressedBlock(input, output, circularBuffer);
            }
        }

        private void HandleUncompressedBlock(Stream input, Stream output, CircularBuffer circularBuffer)
        {
            var next = input.ReadByte();
            if (next < 0)
                throw new StreamTooShortException();

            output.WriteByte((byte)next);
            circularBuffer.WriteByte((byte)next);
        }

        private void HandleCompressedBlock(Stream input, Stream output, CircularBuffer circularBuffer)
        {
            // A compressed block starts with 2 bytes; if there are there < 2 bytes left, throw error
            if (input.Length - input.Position < 2)
                throw new StreamTooShortException();

            var byte1 = input.ReadByte();
            var byte2 = input.ReadByte();

            // The number of bytes to copy
            var length = (byte2 >> 4) + 2;

            // From where the bytes should be copied (relatively)
            var displacement = (((byte2 & 0x0F) << 8) | byte1) + 1;

            if (displacement > output.Length)
                throw new DisplacementException(displacement, output.Length, input.Position - 2);

            circularBuffer.Copy(output, displacement, length);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
