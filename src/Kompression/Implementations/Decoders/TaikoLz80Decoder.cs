using System.IO;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Decoders
{
    class TaikoLz80Decoder : IPatternMatchDecoder
    {
        private byte[] _windowBuffer;
        private int _windowBufferOffset;

        public void Decode(Stream input, Stream output)
        {
            _windowBuffer = new byte[0x8000];
            _windowBufferOffset = 0;

            bool isFinished = false;
            while (input.Position < input.Length)
            {
                var code = input.ReadByte();
                switch (code >> 6)
                {
                    case 0:
                        isFinished = ReadUncompressedData(input, output, code);
                        break;
                    case 1:
                        ReadOneCompressedData(output, code);
                        break;
                    case 2:
                        ReadTwoCompressedData(input, output, code);
                        break;
                    case 3:
                        ReadThreeCompressedData(input, output, code);
                        break;
                }

                if(isFinished)
                    break;
            }
        }

        private bool ReadUncompressedData(Stream input, Stream output, int code)
        {
            var length = code & 0x3F;
            if (code == 0)
            {
                length = 0x40;

                var byte1 = input.ReadByte();
                if (byte1 >> 7 == 0)
                {
                    length = 0xbf;

                    var byte2 = input.ReadByte();
                    // If both bytes are 0, that's the end flag; Communicate this flag back to the loop
                    if (byte1 == 0 && byte2 == 0)
                        return true;

                    length += (byte1 << 8) | byte2;
                }
                else
                {
                    length += byte1 & 0x7F;
                }
            }

            for (var i = 0; i < length; i++)
            {
                var next = (byte)input.ReadByte();
                output.WriteByte(next);
                _windowBuffer[_windowBufferOffset] = next;
                _windowBufferOffset = (_windowBufferOffset + 1) % _windowBuffer.Length;
            }

            return false;
        }

        private void ReadOneCompressedData(Stream output, int code)
        {
            // 8 bits
            // 11 11 1111

            var length = ((code >> 4) & 0x3) + 2;
            var displacement = (code & 0xF) + 1;

            ReadDisplacedData(output, displacement, length);
        }

        private void ReadTwoCompressedData(Stream input, Stream output, int code)
        {
            // 16 bits
            // 11 1111 1111111111

            var byte1 = input.ReadByte();

            var length = ((code >> 2) & 0xF) + 3;
            var displacement = (((code & 0x3) << 8) | byte1) + 1;

            ReadDisplacedData(output, displacement, length);
        }

        private void ReadThreeCompressedData(Stream input, Stream output, int code)
        {
            // 24 bits
            // 11 1111111 111111111111111

            var byte1 = input.ReadByte();
            var byte2 = input.ReadByte();

            var length = (((code & 0x3F) << 1) | (byte1 >> 7)) + 4;
            var displacement = (((byte1 & 0x7F) << 8) | byte2) + 1;

            ReadDisplacedData(output, displacement, length);
        }

        private void ReadDisplacedData(Stream output, int displacement, int length)
        {
            var bufferIndex = _windowBufferOffset + _windowBuffer.Length - displacement;
            for (var i = 0; i < length; i++)
            {
                var next = _windowBuffer[bufferIndex++ % _windowBuffer.Length];
                output.WriteByte(next);
                _windowBuffer[_windowBufferOffset] = next;
                _windowBufferOffset = (_windowBufferOffset + 1) % _windowBuffer.Length;
            }
        }

        public void Dispose()
        {
            _windowBuffer = null;
        }
    }
}
