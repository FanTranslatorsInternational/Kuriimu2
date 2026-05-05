using System.Buffers.Binary;
using Kompression.Contract.Decoder;
using Kompression.Exceptions;
using Kompression.IO;

/* https://github.com/IcySon55/Kuriimu/issues/438 */
/* Kanken Training 2 */

namespace Kompression.Decoder
{
    public class LzEcdDecoder : IDecoder
    {
        private const int PreBufferSize_ = 0x3BE;

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[12];

            _ = input.Read(buffer.AsSpan(0, 3));
            if (buffer[0] is not 0x45 || buffer[1] is not 0x43 || buffer[2] is not 0x44)
                throw new InvalidCompressionException("LZ ECD");

            if (input.ReadByte() == 0)
            {
                input.Position = 0x10;
                input.CopyTo(output);
                return;
            }

            _ = input.Read(buffer);
            int skipData = BinaryPrimitives.ReadInt32BigEndian(buffer);
            _ = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(4));
            int uncompressedLength = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(8));

            var circularBuffer = new CircularBuffer(0x400)
            {
                Position = PreBufferSize_
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

                var flag = codeBlock >> codeBlockPosition++ & 0x1;
                if (flag == 1)
                {
                    var value = (byte)input.ReadByte();
                    output.WriteByte(value);
                    circularBuffer.WriteByte(value);
                }
                else
                {
                    var byte1 = input.ReadByte();
                    var byte2 = input.ReadByte();

                    var length = (byte2 & 0x3F) + 3;
                    var bufferPosition = (byte2 & 0xC0) << 2 | byte1;

                    // Convert buffer position to displacement
                    var displacement = (circularBuffer.Position - bufferPosition) % circularBuffer.Length;
                    displacement = (displacement + circularBuffer.Length) % circularBuffer.Length;
                    if (displacement == 0)
                        displacement = 0x400;

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
