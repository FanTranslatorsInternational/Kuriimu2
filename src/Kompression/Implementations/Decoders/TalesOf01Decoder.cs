using System;
using System.IO;
using Kompression.Configuration;
using Kompression.IO;

namespace Kompression.Implementations.Decoders
{
    public class TalesOf01Decoder : IDecoder
    {
        private CircularBuffer _circularBuffer;
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
            var compressedDataSize = GetLittleEndian(buffer);
            input.Read(buffer, 0, 4);
            var decompressedSize = GetLittleEndian(buffer);

            _circularBuffer = new CircularBuffer(0x1000)
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
                    _circularBuffer.WriteByte(value);
                }
                else
                {
                    // compressed data
                    var byte1 = input.ReadByte();
                    var byte2 = input.ReadByte();

                    var length = (byte2 & 0xF) + 3;
                    var bufferPosition = byte1 | ((byte2 & 0xF0) << 4);

                    // Convert buffer position to displacement
                    var displacement = _circularBuffer.Position % _circularBuffer.Length - bufferPosition;

                    _circularBuffer.Copy(output, displacement, length);
                }
            }
        }

        private int GetLittleEndian(byte[] data)
        {
            return (data[3] << 24) | (data[2] << 16) | (data[1] << 8) | data[0];
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
