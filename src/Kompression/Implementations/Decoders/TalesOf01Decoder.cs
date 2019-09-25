using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Decoders
{
    class TalesOf01Decoder : IPatternMatchDecoder
    {
        private byte[] _windowBuffer;
        private int _windowBufferOffset;

        public void Decode(Stream input, Stream output)
        {
            if (input.ReadByte() != 0x01)
                throw new InvalidOperationException("This is not a tales of compression with version 1.");

            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            var compressedDataSize = GetLittleEndian(buffer);
            input.Read(buffer, 0, 4);
            var decompressedSize = GetLittleEndian(buffer);

            _windowBuffer = new byte[0x1000];
            _windowBufferOffset = 0xFEE;

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
                    _windowBuffer[_windowBufferOffset++ % _windowBuffer.Length] = value;
                }
                else
                {
                    // compressed data
                    var byte1 = input.ReadByte();
                    var byte2 = input.ReadByte();

                    var length = (byte2 & 0xF) + 3;
                    var bufferPosition = byte1 | ((byte2 & 0xF0) << 4);

                    for (var i = 0; i < length; i++)
                    {
                        var value = _windowBuffer[(bufferPosition + i) % _windowBuffer.Length];
                        output.WriteByte(value);
                        _windowBuffer[_windowBufferOffset++ % _windowBuffer.Length] = value;
                    }
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
