using System;
using System.IO;
using Kompression.Extensions;
using Kompression.IO;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class TalesOf01Decoder : IDecoder
    {
        private int _preBufferSize;

        public TalesOf01Decoder(int preBufferSize)
        {
            _preBufferSize = preBufferSize;
        }

        public void Decode(Stream input, Stream output)
        {
            if (input.ReadByte() != 0x01)
                throw new InvalidOperationException("This is not a tales of compression with version 1.");

            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            var compressedDataSize =buffer.GetInt32LittleEndian(0);
            input.Read(buffer, 0, 4);
            var decompressedSize = buffer.GetInt32LittleEndian(0);

            var circularBuffer = new CircularBuffer(0x1000)
            {
                Position = _preBufferSize
            };

            var flags = 0;
            var flagPosition = 8;
            while (output.Length < decompressedSize)
            {
                if (flagPosition == 8)
                {
                    flagPosition = 0;
                    flags = input.ReadByte();
                }

                if (((flags >> flagPosition++) & 0x1) == 1)
                {
                    // raw data
                    var value = (byte)input.ReadByte();

                    output.WriteByte(value);
                    circularBuffer.WriteByte(value);
                }
                else
                {
                    // compressed data
                    var byte1 = input.ReadByte();
                    var byte2 = input.ReadByte();

                    var length = (byte2 & 0xF) + 3;
                    var bufferPosition = byte1 | ((byte2 & 0xF0) << 4);

                    // Convert buffer position to displacement
                    var displacement = (circularBuffer.Position - bufferPosition) % circularBuffer.Length;
                    displacement = (displacement + circularBuffer.Length) % circularBuffer.Length;
                    if (displacement == 0)
                        displacement = 0x1000;

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
