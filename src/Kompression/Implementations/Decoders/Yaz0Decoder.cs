using System.IO;
using System.Linq;
using Kompression.Exceptions;
using Kompression.Extensions;
using Kompression.IO;
using Kontract.Kompression.Configuration;
using Kontract.Models.IO;

namespace Kompression.Implementations.Decoders
{
    public class Yaz0Decoder : IDecoder
    {
        private readonly ByteOrder _byteOrder;

        public Yaz0Decoder(ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
        }

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            if (!buffer.SequenceEqual(new byte[] { 0x59, 0x61, 0x7a, 0x30 }))
                throw new InvalidCompressionException("Yaz0" + (_byteOrder == ByteOrder.LittleEndian ? "LE" : "BE"));

            input.Read(buffer, 0, 4);
            var uncompressedLength =
                _byteOrder == ByteOrder.LittleEndian ? buffer.GetInt32LittleEndian(0) : buffer.GetInt32BigEndian(0);
            input.Position += 0x8;

            var circularBuffer = new CircularBuffer(0x1000);

            var codeBlock = input.ReadByte();
            var codeBlockPosition = 8;
            while (output.Length < uncompressedLength)
            {
                if (codeBlockPosition == 0)
                {
                    codeBlockPosition = 8;
                    codeBlock = input.ReadByte();
                }

                var flag = (codeBlock >> --codeBlockPosition) & 0x1;
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

                    var displacement = (((firstByte & 0xF) << 8) | secondByte) + 1;

                    circularBuffer.Copy(output, displacement, length);
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
