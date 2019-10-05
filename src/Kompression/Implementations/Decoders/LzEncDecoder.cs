using System.IO;
using Kompression.Configuration;
using Kompression.IO;

namespace Kompression.Implementations.Decoders
{
    // TODO: LzEncEncoder needed
    public class LzEncDecoder : IDecoder
    {
        private CircularBuffer _circularBuffer;

        private byte _codeByte;

        public void Decode(Stream input, Stream output)
        {
            _circularBuffer = new CircularBuffer(0xBFFF);

            // Handle initial byte
            _codeByte = (byte)input.ReadByte();
            if (_codeByte < 0x12)
            {
                input.Position--;
                if (!ReadSubLoopRawData(input, output))
                    if (!ReadSubLoopMatch(input, output))
                        SubLoop(input, output);
            }
            else
            {
                var count = _codeByte - 0x11;
                if (count > 3)
                {
                    ReadRawData(input, output, count);
                    if (!ReadSubLoopMatch(input, output))
                        SubLoop(input, output);
                }
                else
                {
                    ReadRawData(input, output, count);
                }
            }

            MainLoop(input, output);
        }

        private void MainLoop(Stream input, Stream output)
        {
            while (true)
            {
                _codeByte = (byte)input.ReadByte();

                if (_codeByte >= 0x40)
                {
                    var byte2 = (byte)input.ReadByte();

                    // Min disp: 0x1; Max disp: 0x800
                    // Min length: 1; Max length: 6

                    var displacement = ((_codeByte >> 2) & 0x7) + (byte2 << 3) + 1;
                    var length = (_codeByte >> 5) + 1;

                    _circularBuffer.Copy(output, displacement, length);
                }
                else if (_codeByte >= 0x20)
                {
                    // 14 bit displacement
                    // Min disp: 1; Max disp: 0x4000
                    // Min length: 2; Max length: virtually infinite

                    var length = _codeByte & 0x1F;
                    if (length == 0)
                        length += ReadVariableLength(input, 5);
                    length += 2;

                    _codeByte = (byte)input.ReadByte();
                    var byte2 = (byte)input.ReadByte();
                    var displacement = (_codeByte >> 2) + (byte2 << 6) + 1;

                    _circularBuffer.Copy(output, displacement, length);
                }
                else if (_codeByte >= 0x10)
                {
                    var length = _codeByte & 0x7;
                    if (length == 0)
                        length += ReadVariableLength(input, 3);
                    length += 2;

                    var sign = _codeByte & 0x8;
                    _codeByte = (byte)input.ReadByte();
                    var byte2 = (byte)input.ReadByte();

                    if (sign == 0 && (_codeByte >> 2) == 0 && byte2 == 0)
                        // End of decompression
                        break;

                    var displacement = (_codeByte >> 2) + (byte2 << 6) + (sign > 0 ? 0x4000 : 0) + 0x4000;

                    _circularBuffer.Copy(output, displacement, length);
                }
                else
                {
                    var byte2 = (byte)input.ReadByte();

                    var displacement = ((byte2 << 2) | (_codeByte >> 2)) + 1;

                    _circularBuffer.Copy(output, displacement, 2);
                }

                SubLoop(input, output);
            }
        }

        private void SubLoop(Stream input, Stream output)
        {
            while (true)
            {
                if ((_codeByte & 0x3) != 0)
                {
                    ReadRawData(input, output, _codeByte & 0x3);
                    break;
                }

                if (ReadSubLoopRawData(input, output))
                    break;

                if (ReadSubLoopMatch(input, output))
                    break;
            }
        }

        private bool ReadSubLoopRawData(Stream input, Stream output)
        {
            // Raw Data read
            _codeByte = (byte)input.ReadByte();
            if (_codeByte > 0xF)
            {
                input.Position--;
                return true;
            }

            var length = _codeByte + 3;
            if (_codeByte == 0)
                length += ReadVariableLength(input, 4);
            ReadRawData(input, output, length);

            return false;
        }

        private bool ReadSubLoopMatch(Stream input, Stream output)
        {
            // Read LZ match
            _codeByte = (byte)input.ReadByte();
            if (_codeByte > 0xF)
            {
                input.Position--;
                return true;
            }

            var codeByte2 = (byte)input.ReadByte();
            var displacement = ((codeByte2 << 2) | (_codeByte >> 2)) + 0x801;

            _circularBuffer.Copy(output, displacement, 3);

            return false;
        }

        private void ReadRawData(Stream input, Stream output, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var value = (byte)input.ReadByte();

                output.WriteByte(value);
                _circularBuffer.WriteByte(value);
            }
        }

        private int ReadVariableLength(Stream input, int bitCount)
        {
            var length = 0;
            var flag = input.ReadByte();
            while (flag == 0)
            {
                length += 0xFF;
                flag = input.ReadByte();
            }

            return length + flag + ((1 << bitCount) - 1);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
