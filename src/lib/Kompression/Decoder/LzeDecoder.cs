using System.Buffers.Binary;
using Kompression.Contract.Decoder;
using Kompression.Exceptions;
using Kompression.IO;

namespace Kompression.Decoder
{
    public class LzeDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];

            _ = input.Read(buffer.AsSpan(0,2));
            if (buffer[0] is not 0x4c || buffer[1] is not 0x65)
                throw new InvalidCompressionException("Lze");

            _ = input.Read(buffer);
            int decompressedSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);

            var circularBuffer = new CircularBuffer(0x1004);
            ReadCompressedData(input, output, circularBuffer, decompressedSize);
        }

        private static void ReadCompressedData(Stream input, Stream output, CircularBuffer circularBuffer, int decompressedSize)
        {
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

                switch (flags >> (readFlags << 1) & 0x3)
                {
                    case 0:
                        // LZS4C
                        HandleZeroCompressedBlock(input, output, circularBuffer);
                        break;
                    case 1:
                        // LZS62
                        HandleOneCompressedBlock(input, output, circularBuffer);
                        break;
                    case 2:
                        HandleCopyBlock(input, output, circularBuffer, 1);
                        break;
                    case 3:
                        HandleCopyBlock(input, output, circularBuffer, 3);
                        break;
                }
            }
        }

        private static void HandleZeroCompressedBlock(Stream input, Stream output, CircularBuffer circularBuffer)
        {
            var byte1 = input.ReadByte();
            var byte2 = input.ReadByte();

            var length = (byte2 >> 4) + 3;
            var displacement = ((byte2 & 0x0F) << 8 | byte1) + 5;

            circularBuffer.Copy(output, displacement, length);
        }

        private static void HandleOneCompressedBlock(Stream input, Stream output, CircularBuffer circularBuffer)
        {
            var byte1 = input.ReadByte();

            var length = (byte1 >> 2) + 2;
            var displacement = (byte1 & 0x3) + 1;

            circularBuffer.Copy(output, displacement, length);
        }

        private static void HandleCopyBlock(Stream input, Stream output, CircularBuffer circularBuffer, int toCopy)
        {
            for (var i = 0; i < toCopy; i++)
            {
                var next = (byte)input.ReadByte();

                output.WriteByte(next);
                circularBuffer.WriteByte(next);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
