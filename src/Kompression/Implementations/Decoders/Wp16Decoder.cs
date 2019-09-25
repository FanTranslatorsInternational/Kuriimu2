using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Decoders
{
    class Wp16Decoder : IPatternMatchDecoder
    {
        private byte[] _windowBuffer;
        private int _windowBufferOffset;

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            if (Encoding.ASCII.GetString(buffer) != "Wp16")
                throw new InvalidOperationException("Not Wp16 compressed.");

            input.Read(buffer, 0, 4);
            var decompressedSize = GetLittleEndian(buffer);

            _windowBuffer = new byte[0x7FF * 2];
            _windowBufferOffset = 0;

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
                    _windowBuffer[_windowBufferOffset++ % _windowBuffer.Length] = value;

                    value = (byte)input.ReadByte();
                    output.WriteByte(value);
                    _windowBuffer[_windowBufferOffset++ % _windowBuffer.Length] = value;
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

                    var bufferIndex = _windowBufferOffset + _windowBuffer.Length - displacement * 2;
                    for (var i = 0; i < length * 2; i++)
                    {
                        var value = _windowBuffer[bufferIndex++ % _windowBuffer.Length];
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
            Array.Clear(_windowBuffer, 0, _windowBuffer.Length);
            _windowBuffer = null;
        }
    }
}
