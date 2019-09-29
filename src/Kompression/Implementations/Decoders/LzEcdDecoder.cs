using System.IO;
using System.Linq;
using Kompression.Configuration;
using Kompression.Exceptions;
using Kompression.IO;
using Kompression.PatternMatch;

/* https://github.com/IcySon55/Kuriimu/issues/438 */
/* Kanken Training 2 */

namespace Kompression.Implementations.Decoders
{
    public class LzEcdDecoder : IDecoder
    {
        private CircularBuffer _circularBuffer;
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

            _circularBuffer = new CircularBuffer(0x400)
            {
                Position = _preBufferLength
            };

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
                    output.WriteByte(value);
                    _circularBuffer.WriteByte(value);
                }
                else
                {
                    var byte1 = input.ReadByte();
                    var byte2 = input.ReadByte();

                    var length = (byte2 & 0x3F) + 3;
                    var matchBufferPosition = ((byte2 & 0xC0) << 2) | byte1;

                    // Convert buffer position to displacement
                    var displacement = _circularBuffer.Position % _circularBuffer.Length - matchBufferPosition;

                    _circularBuffer.Copy(output, displacement, length);
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
