using System.Diagnostics;
using System.IO;
using Kompression.Configuration;
using Kompression.Exceptions;
using Kompression.Implementations;
using Kompression.IO;
using Kompression.PatternMatch;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class Lz10Decoder : IDecoder
    {
        private CircularBuffer _circularBuffer;

        public void Decode(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x10)
                throw new InvalidCompressionException("LZ10");

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            ReadCompressedData(input, output, decompressedSize);
        }

        internal void ReadCompressedData(Stream input, Stream output, int decompressedSize)
        {
            _circularBuffer = new CircularBuffer(0x1000);

            int flags = 0, mask = 1;
            while (output.Length < decompressedSize)
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
                    HandleCompressedBlock(input, output);
                else
                    HandleUncompressedBlock(input, output);
            }
        }

        private void HandleUncompressedBlock(Stream input, Stream output)
        {
            var next = input.ReadByte();
            if (next < 0)
                throw new StreamTooShortException();

            output.WriteByte((byte)next);
            _circularBuffer.WriteByte((byte)next);
        }

        private void HandleCompressedBlock(Stream input, Stream output)
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

            _circularBuffer.Copy(output, displacement, length);
        }

        public void Dispose()
        {
            _circularBuffer?.Dispose();
        }
    }
}
