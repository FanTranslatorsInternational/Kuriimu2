using System.IO;
using Kompression.Exceptions;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Decoders
{
    class LzeDecoder : IPatternMatchDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];
            input.Read(buffer, 0, 2);
            if (buffer[0] != 0x4c || buffer[1] != 0x65)
                throw new InvalidCompressionException(nameof(Lze));

            input.Read(buffer, 0, 4);
            var decompressedSize = GetLittleEndian(buffer);

            ReadCompressedData(input, output, decompressedSize);
        }

        private void ReadCompressedData(Stream input, Stream output, int decompressedSize)
        {
            int bufferLength = 0x1004, bufferOffset = 0;
            byte[] buffer = new byte[bufferLength];

            int flags = 0, readFlags = 3;
            while (output.Length < decompressedSize)
            {
                if (readFlags == 3)
                {
                    flags = input.ReadByte();
                    readFlags = 0;
                }
                else
                {
                    readFlags++;
                }

                switch ((flags >> (readFlags << 1)) & 0x3)
                {
                    case 0:
                        // LZS4C
                        bufferOffset = HandleZeroCompressedBlock(input, output, buffer, bufferOffset);
                        break;
                    case 1:
                        // LZS62
                        bufferOffset = HandleOneCompressedBlock(input, output, buffer, bufferOffset);
                        break;
                    case 2:
                        bufferOffset = HandleCopyBlock(input, output, buffer, bufferOffset, 1);
                        break;
                    case 3:
                        bufferOffset = HandleCopyBlock(input, output, buffer, bufferOffset, 3);
                        break;
                }
            }
        }

        private int HandleZeroCompressedBlock(Stream input, Stream output, byte[] windowBuffer, int windowBufferOffset)
        {
            var byte1 = input.ReadByte();
            var byte2 = input.ReadByte();

            var length = (byte2 >> 4) + 3;
            var displacement = (((byte2 & 0x0F) << 8) | byte1) + 5;

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

        private int HandleOneCompressedBlock(Stream input, Stream output, byte[] windowBuffer, int windowBufferOffset)
        {
            var byte1 = input.ReadByte();

            var length = (byte1 >> 2) + 2;
            var displacement = (byte1 & 0x3) + 1;

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

        private int HandleCopyBlock(Stream input, Stream output, byte[] windowBuffer, int windowBufferOffset, int toCopy)
        {
            for (var i = 0; i < toCopy; i++)
            {
                var next = (byte)input.ReadByte();
                output.WriteByte(next);
                windowBuffer[windowBufferOffset] = next;
                windowBufferOffset = (windowBufferOffset + 1) % windowBuffer.Length;
            }

            return windowBufferOffset;
        }

        private int GetLittleEndian(byte[] data)
        {
            return (data[3] << 24) | (data[2] << 16) | (data[1] << 8) | data[0];
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
