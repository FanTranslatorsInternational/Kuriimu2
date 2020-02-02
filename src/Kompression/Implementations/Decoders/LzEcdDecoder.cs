using System.IO;
using System.Linq;
using Kompression.Configuration;
using Kompression.Exceptions;
using Kompression.Extensions;
using Kompression.IO;
using Kontract.Kompression.Configuration;

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
            var skipData = buffer.GetInt32BigEndian(0);
            input.Read(buffer, 0, 4);
            var compressedLength = buffer.GetInt32BigEndian(0);
            input.Read(buffer, 0, 4);
            var uncompressedLength = buffer.GetInt32BigEndian(0);

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
                    var bufferPosition = ((byte2 & 0xC0) << 2) | byte1;

                    // Convert buffer position to displacement
                    var displacement = (_circularBuffer.Position - bufferPosition) % _circularBuffer.Length;
                    displacement = (displacement + _circularBuffer.Length) % _circularBuffer.Length;
                    if (displacement == 0)
                        displacement = 0x400;

                    _circularBuffer.Copy(output, displacement, length);
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
