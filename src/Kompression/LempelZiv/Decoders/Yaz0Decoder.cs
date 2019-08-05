using System.IO;
using System.Linq;
using Kompression.Exceptions;

namespace Kompression.LempelZiv.Decoders
{
    public class Yaz0Decoder : ILzDecoder
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
                _byteOrder == ByteOrder.LittleEndian ? GetLittleEndian(buffer) : GetBigEndian(buffer);
            input.Position += 0x8;

            var windowBuffer = new byte[0x1000];
            var windowBufferPosition = 0;

            var codeBlock = input.ReadByte();
            var codeBlockPosition = 8;
            while (output.Length < uncompressedLength)
            {
                if (codeBlockPosition == 0)
                {
                    codeBlockPosition = 8;
                    codeBlock = input.ReadByte();
                }

                if (codeBlock >> --codeBlockPosition == 1)
                {
                    // Flag for uncompressed byte
                    var value = (byte)input.ReadByte();
                    windowBuffer[windowBufferPosition++ % windowBuffer.Length] = value;
                    output.WriteByte(value);
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

                    var bufferIndex = windowBufferPosition + windowBuffer.Length - displacement;
                    for (var i = 0; i < length; i++)
                    {
                        var value = windowBuffer[bufferIndex++ % windowBuffer.Length];
                        output.WriteByte(value);
                        windowBuffer[windowBufferPosition++ % windowBuffer.Length] = value;
                    }
                }
            }
        }

        private int GetLittleEndian(byte[] data)
        {
            return data[3] | data[2] | data[1] | data[0];
        }

        private int GetBigEndian(byte[] data)
        {
            return data[0] | data[1] | data[2] | data[3];
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
