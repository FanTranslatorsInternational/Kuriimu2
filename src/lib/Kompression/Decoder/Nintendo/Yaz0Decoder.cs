using Komponent.Contract.Enums;
using Komponent.IO;
using Kompression.Contract.Decoder;
using Kompression.Exceptions;
using Kompression.IO;

namespace Kompression.Decoder.Nintendo
{
    public class Yaz0Decoder(ByteOrder byteOrder) : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];

            _ = input.Read(buffer);
            if (buffer[0] is not 0x59 || buffer[1] is not 0x61 || buffer[2] is not 0x7A || buffer[3] is not 0x30)
                throw new InvalidCompressionException("Yaz0" + (byteOrder == ByteOrder.LittleEndian ? "LE" : "BE"));

            using var br = new BinaryReaderX(input, true, byteOrder);

            int uncompressedLength = br.ReadInt32();
            input.Position += 0x8;

            var circularBuffer = new CircularBuffer(0x1000);

            int codeBlock = input.ReadByte();
            var codeBlockPosition = 8;
            while (output.Length < uncompressedLength)
            {
                if (codeBlockPosition == 0)
                {
                    codeBlockPosition = 8;
                    codeBlock = input.ReadByte();
                }

                var flag = codeBlock >> --codeBlockPosition & 0x1;
                if (flag == 1)
                {
                    // Flag for uncompressed byte
                    var value = (byte)input.ReadByte();

                    output.WriteByte(value);
                    circularBuffer.WriteByte(value);
                }
                else
                {
                    var firstByte = input.ReadByte();
                    var secondByte = input.ReadByte();

                    var length = firstByte >> 4;
                    if (length > 0)
                        length += 2;
                    else
                    {
                        // Yes, we do read the length from the uncompressed data stream
                        length = input.ReadByte() + 0x12;
                    }

                    var displacement = ((firstByte & 0xF) << 8 | secondByte) + 1;

                    circularBuffer.Copy(output, displacement, length);
                }
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
