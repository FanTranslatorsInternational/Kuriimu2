using System.IO;
using Kompression.Exceptions;
using Kompression.Implementations;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Decoders
{
    class Lz40Decoder: IPatternMatchDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x40)
                throw new InvalidCompressionException(nameof(LZ40));

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            ReadCompressedData(input, output, decompressedSize);
        }

        internal void ReadCompressedData(Stream input, Stream output, int decompressedSize)
        {
            int bufferLength = 0xFFF, bufferOffset = 0;
            byte[] buffer = new byte[bufferLength];

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

                bufferOffset = (flags & mask) > 0 ?
                    HandleCompressedBlock(input, output, buffer, bufferOffset) :
                    HandleUncompressedBlock(input, output, buffer, bufferOffset);
            }
        }

        private int HandleUncompressedBlock(Stream input, Stream output, byte[] windowBuffer, int windowBufferOffset)
        {
            var next = input.ReadByte();
            if (next < 0)
                throw new StreamTooShortException();

            output.WriteByte((byte)next);
            windowBuffer[windowBufferOffset] = (byte)next;
            return (windowBufferOffset + 1) % windowBuffer.Length;
        }

        private int HandleCompressedBlock(Stream input, Stream output, byte[] windowBuffer, int windowBufferOffset)
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
                length = HandleZeroCompressedBlock(input, output);
            }
            else if ((byte1 & 0xF) == 1)   // 0001
            {
                length = HandleOneCompressedBlock(input, output);
            }
            else    // >= 0010
            {
                length = byte1 & 0xF;
            }

            var bufferIndex = windowBufferOffset + windowBuffer.Length - displacement;
            for (var i = 0; i < length; i++)
            {
                var next = windowBuffer[bufferIndex++ % windowBuffer.Length];
                output.WriteByte(next);
                windowBuffer[windowBufferOffset] = next;
                windowBufferOffset = (windowBufferOffset + 1) % windowBuffer.Length;
            }

            return windowBufferOffset;
        }

        private int HandleZeroCompressedBlock(Stream input, Stream output)
        {
            if (input.Length - input.Position < 1)
                throw new StreamTooShortException();

            var byte3 = input.ReadByte();
            var length = byte3 + 0x10;  // max 0xFF + 0x10 = 0x10F

            return length;
        }

        private int HandleOneCompressedBlock(Stream input, Stream output)
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
            // Nothing to dispose
        }
    }
}
