using System.IO;
using Kompression.Configuration;
using Kompression.Exceptions;
using Kompression.Extensions;
using Kompression.IO;

namespace Kompression.Implementations.Decoders
{
    public class LzeDecoder : IDecoder
    {
        private CircularBuffer _circularBuffer;

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];
            input.Read(buffer, 0, 2);
            if (buffer[0] != 0x4c || buffer[1] != 0x65)
                throw new InvalidCompressionException("Lze");

            input.Read(buffer, 0, 4);
            var decompressedSize = buffer.GetInt32LittleEndian(0);

            _circularBuffer = new CircularBuffer(0x1004);
            ReadCompressedData(input, output, decompressedSize);
        }

        private void ReadCompressedData(Stream input, Stream output, int decompressedSize)
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

                switch ((flags >> (readFlags << 1)) & 0x3)
                {
                    case 0:
                        // LZS4C
                        HandleZeroCompressedBlock(input, output);
                        break;
                    case 1:
                        // LZS62
                        HandleOneCompressedBlock(input, output);
                        break;
                    case 2:
                        HandleCopyBlock(input, output, 1);
                        break;
                    case 3:
                        HandleCopyBlock(input, output, 3);
                        break;
                }
            }
        }

        private void HandleZeroCompressedBlock(Stream input, Stream output)
        {
            var byte1 = input.ReadByte();
            var byte2 = input.ReadByte();

            var length = (byte2 >> 4) + 3;
            var displacement = (((byte2 & 0x0F) << 8) | byte1) + 5;

            _circularBuffer.Copy(output, displacement, length);
        }

        private void HandleOneCompressedBlock(Stream input, Stream output)
        {
            var byte1 = input.ReadByte();

            var length = (byte1 >> 2) + 2;
            var displacement = (byte1 & 0x3) + 1;

            _circularBuffer.Copy(output, displacement, length);
        }

        private void HandleCopyBlock(Stream input, Stream output, int toCopy)
        {
            for (var i = 0; i < toCopy; i++)
            {
                var next = (byte)input.ReadByte();

                output.WriteByte(next);
                _circularBuffer.WriteByte(next);
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
