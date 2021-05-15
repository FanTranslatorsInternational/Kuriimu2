using System.IO;
using Kompression.Exceptions;
using Kompression.IO;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders.Headerless
{
    public class Lz10HeaderlessDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            Decode(input, output, -1);
        }

        public void Decode(Stream input, Stream output, int decompressedSize)
        {
            var circularBuffer = new CircularBuffer(0x1000);

            int flags = 0, mask = 1;
            while (ShouldContinue(input, output, decompressedSize))
            {
                if (mask == 1)
                {
                    flags = input.ReadByte();
                    if (flags < 0)
                        throw new StreamTooShortException();
                    mask = 0x80;
                }
                else
                {
                    mask >>= 1;
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
            var length = (byte1 >> 4) + 3;

            // From where the bytes should be copied (relatively)
            var displacement = (((byte1 & 0x0F) << 8) | byte2) + 1;

            if (displacement > output.Length)
                throw new DisplacementException(displacement, output.Length, input.Position - 2);

            circularBuffer.Copy(output, displacement, length);
        }

        private bool ShouldContinue(Stream input, Stream output, int decompressedSize)
        {
            if (decompressedSize < 0)
                return input.Position < input.Length;

            return output.Length < decompressedSize;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
