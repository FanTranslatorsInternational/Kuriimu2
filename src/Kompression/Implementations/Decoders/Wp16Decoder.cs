using System;
using System.IO;
using System.Text;
using Kompression.Configuration;
using Kompression.IO;

namespace Kompression.Implementations.Decoders
{
    public class Wp16Decoder : IDecoder
    {
        private CircularBuffer _circularBuffer;

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            if (Encoding.ASCII.GetString(buffer) != "Wp16")
                throw new InvalidOperationException("Not Wp16 compressed.");

            input.Read(buffer, 0, 4);
            var decompressedSize = GetLittleEndian(buffer);

            _circularBuffer = new CircularBuffer(0xFFE);

            long flags = 0;
            var flagPosition = 32;
            while (output.Length < decompressedSize)
            {
                if (flagPosition == 32)
                {
                    input.Read(buffer, 0, 4);
                    flags = GetLittleEndian(buffer);
                    flagPosition = 0;
                }

                if (((flags >> flagPosition++) & 0x1) == 1)
                {
                    // Copy 2 bytes from input

                    var value = (byte)input.ReadByte();
                    output.WriteByte(value);
                    _circularBuffer.WriteByte(value);

                    value = (byte)input.ReadByte();
                    output.WriteByte(value);
                    _circularBuffer.WriteByte(value);
                }
                else
                {
                    // Read the Lz match
                    // min displacement 2, max displacement 0xFFE
                    // min length 2, max length 0x42

                    var byte1 = input.ReadByte();
                    var byte2 = input.ReadByte();

                    var displacement = (byte2 << 3) | (byte1 >> 5);
                    var length = (byte1 & 0x1F) + 2;

                    _circularBuffer.Copy(output, displacement, length * 2);
                }
            }
        }

        private int GetLittleEndian(byte[] data)
        {
            return (data[3] << 24) | (data[2] << 16) | (data[1] << 8) | data[0];
        }

        public void Dispose()
        {
            _circularBuffer?.Dispose();
            _circularBuffer = null;
        }
    }
}
