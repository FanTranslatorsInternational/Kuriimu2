using System.IO;
using Kompression.Exceptions;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Decoders
{
    class Lz11Decoder: IPatternMatchDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x11)
                throw new InvalidCompressionException(nameof(LZ11));

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            ReadCompressedData(input, output, decompressedSize);
        }
        private void ReadCompressedData(Stream input, Stream output, int decompressedSize)
        {
            int bufferLength = 0x1000, bufferOffset = 0;
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

            int length, displacement;
            if (byte1 >> 4 == 0)    // 0000
            {
                (length, displacement) = HandleZeroCompressedBlock(byte1, byte2, input, output);
            }
            else if (byte1 >> 4 == 1)   // 0001
            {
                (length, displacement) = HandleOneCompressedBlock(byte1, byte2, input, output);
            }
            else    // >= 0010
            {
                (length, displacement) = HandleRemainingCompressedBlock(byte1, byte2, input, output);
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

        private (int length, int displacement) HandleZeroCompressedBlock(byte byte1, byte byte2, Stream input, Stream output)
        {
            if (input.Length - input.Position < 1)
                throw new StreamTooShortException();

            var byte3 = input.ReadByte();
            var length = (((byte1 & 0xF) << 4) | (byte2 >> 4)) + 0x11;  // max 0xFF + 0x11 = 0x110
            var displacement = (((byte2 & 0xF) << 8) | byte3) + 1;  // max 0xFFF + 1 = 0x1000

            if (displacement > output.Length)
                throw new DisplacementException(displacement, output.Length, input.Position - 3);

            return (length, displacement);
        }

        private (int length, int displacement) HandleOneCompressedBlock(byte byte1, byte byte2, Stream input, Stream output)
        {
            if (input.Length - input.Position < 2)
                throw new StreamTooShortException();

            var byte3 = input.ReadByte();
            var byte4 = input.ReadByte();
            var length = (((byte1 & 0xF) << 12) | (byte2 << 4) | (byte3 >> 4)) + 0x111; // max 0xFFFF + 0x111 = 0x10110
            var displacement = (((byte3 & 0xF) << 8) | byte4) + 1;  // max 0xFFF + 1 = 0x1000

            if (displacement > output.Length)
                throw new DisplacementException(displacement, output.Length, input.Position - 4);

            return (length, displacement);
        }

        private (int length, int displacement) HandleRemainingCompressedBlock(byte byte1, byte byte2, Stream input, Stream output)
        {
            var length = (byte1 >> 4) + 1;  // max 0xF + 1 = 0x10
            var displacement = (((byte1 & 0xF) << 8) | byte2) + 1;   // max 0xFFF + 1 = 0x1000

            if (displacement > output.Length)
                throw new DisplacementException(displacement, output.Length, input.Position - 2);

            return (length, displacement);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
