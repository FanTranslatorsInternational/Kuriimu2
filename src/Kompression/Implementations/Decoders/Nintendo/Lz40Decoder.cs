using System.IO;
using Kompression.Exceptions;
using Kompression.IO;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders.Nintendo
{
    public class Lz40Decoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x40)
                throw new InvalidCompressionException("Lz40");

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            ReadCompressedData(input, output, decompressedSize);
        }

        internal void ReadCompressedData(Stream input, Stream output, int decompressedSize)
        {
            var circularBuffer = new CircularBuffer(0xFFF);

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

            var byte1 = (byte)input.ReadByte();
            var byte2 = (byte)input.ReadByte();

            int displacement = (byte2 << 4) | (byte1 >> 4);    // max 0xFFF
            if (displacement > output.Length)
                throw new DisplacementException(displacement, output.Length, input.Position - 2);

            int length;
            if ((byte1 & 0xF) == 0)    // 0000
            {
                length = HandleZeroCompressedBlock(input);
            }
            else if ((byte1 & 0xF) == 1)   // 0001
            {
                length = HandleOneCompressedBlock(input);
            }
            else    // >= 0010
            {
                length = byte1 & 0xF;
            }

            circularBuffer.Copy(output, displacement, length);
        }

        private int HandleZeroCompressedBlock(Stream input)
        {
            if (input.Length - input.Position < 1)
                throw new StreamTooShortException();

            var byte3 = input.ReadByte();
            var length = byte3 + 0x10;  // max 0xFF + 0x10 = 0x10F

            return length;
        }

        private int HandleOneCompressedBlock(Stream input)
        {
            if (input.Length - input.Position < 2)
                throw new StreamTooShortException();

            var byte3 = input.ReadByte();
            var byte4 = input.ReadByte();
            var length = ((byte4 << 8) | byte3) + 0x110; // max 0xFFFF + 0x110 = 0x1010F

            return length;
        }

        public void Dispose()
        {
        }
    }
}
