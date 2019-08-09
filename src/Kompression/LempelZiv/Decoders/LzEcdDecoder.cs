using System.IO;
using System.Linq;
using Kompression.Exceptions;

/* https://github.com/IcySon55/Kuriimu/issues/438 */
/* Kanken Training 2 */

namespace Kompression.LempelZiv.Decoders
{
    class LzEcdDecoder : ILzDecoder
    {
        private readonly int _preBufferLength;

        public LzEcdDecoder(int preBufferLength)
        {
            _preBufferLength = preBufferLength;
        }

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];
            input.Read(buffer, 0, 3);
            if (!buffer.SequenceEqual(new byte[] { 0x45, 0x43, 0x44, 0x00 }))
                throw new InvalidCompressionException("LZ ECD");
            if (input.ReadByte() == 0)
            {
                input.Position = 0x10;
                input.CopyTo(output);
                return;
            }

            input.Read(buffer, 0, 4);
            var skipData = GetBigEndian(buffer);
            input.Read(buffer, 0, 4);
            var compressedLength = GetBigEndian(buffer);
            input.Read(buffer, 0, 4);
            var uncompressedLength = GetBigEndian(buffer);

            var windowBuffer = new byte[0x400];
            var windowBufferPosition = _preBufferLength;

            // Read initial data
            for (var i = 0; i < skipData; i++)
            {
                var value = (byte)input.ReadByte();
                output.WriteByte(value);
            }

            var codeBlock = input.ReadByte();
            var codeBlockPosition = 0;
            while (output.Length < uncompressedLength)
            {
                if (codeBlockPosition == 8)
                {
                    codeBlockPosition = 0;
                    codeBlock = input.ReadByte();
                }

                var flag = (codeBlock >> codeBlockPosition++) & 0x1;
                if (flag == 1)
                {
                    var value = (byte)input.ReadByte();
                    windowBuffer[windowBufferPosition++ % windowBuffer.Length] = value;
                    output.WriteByte(value);
                }
                else
                {
                    var byte1 = input.ReadByte();
                    var byte2 = input.ReadByte();

                    var length = (byte2 & 0x3F) + 3;
                    var matchBufferPosition = ((byte2 & 0xC0) << 2) | byte1;

                    for (var i = 0; i < length; i++)
                    {
                        var value = windowBuffer[(matchBufferPosition + i) % windowBuffer.Length];
                        windowBuffer[windowBufferPosition++ % windowBuffer.Length] = value;
                        output.WriteByte(value);
                    }
                }
            }
        }

        private int GetBigEndian(byte[] data)
        {
            return (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
