using System.IO;
using Kompression.IO;
using Kontract.Kompression.Interfaces.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class LzEncDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var circularBuffer = new CircularBuffer(0xBFFF);

            // Handle initial byte
            var codeByte = (byte)input.ReadByte();
            if (codeByte < 0x12)
            {
                input.Position--;
                if (!ReadSubLoopRawData(input, output, circularBuffer))
                    if (!ReadSubLoopMatch(input, output, circularBuffer, out codeByte))
                        SubLoop(input, output, circularBuffer, codeByte);
            }
            else
            {
                var count = codeByte - 0x11;
                if (count > 3)
                {
                    ReadRawData(input, output, circularBuffer, count);
                    if (!ReadSubLoopMatch(input, output, circularBuffer, out codeByte))
                        SubLoop(input, output, circularBuffer, codeByte);
                }
                else
                {
                    ReadRawData(input, output, circularBuffer, count);
                }
            }

            MainLoop(input, output, circularBuffer);
        }

        private void MainLoop(Stream input, Stream output, CircularBuffer circularBuffer)
        {
            while (true)
            {
                var codeByte = (byte)input.ReadByte();

                if (codeByte >= 0x40)
                {
                    var byte2 = (byte)input.ReadByte();

                    // 11 bit displacement
                    // Min disp: 0x1; Max disp: 0x800
                    // Min length: 3; Max length: 8

                    var displacement = ((codeByte >> 2) & 0x7) + (byte2 << 3) + 1;
                    var length = (codeByte >> 5) + 1;

                    circularBuffer.Copy(output, displacement, length);
                }
                else if (codeByte >= 0x20)
                {
                    // 14 bit displacement
                    // Min disp: 1; Max disp: 0x4000
                    // Min length: 2; Max length: virtually infinite

                    var length = codeByte & 0x1F;
                    if (length == 0)
                        length += ReadVariableLength(input, 5);
                    length += 2;

                    codeByte = (byte)input.ReadByte();
                    var byte2 = (byte)input.ReadByte();
                    var displacement = (codeByte >> 2) + (byte2 << 6) + 1;

                    circularBuffer.Copy(output, displacement, length);
                }
                else if (codeByte >= 0x10)
                {
                    // 14 bit displacement
                    // Min disp: 0x4001; Max disp: 0xBFFF
                    // Min length: 2; Max length: virtually infinite

                    var length = codeByte & 0x7;
                    if (length == 0)
                        length += ReadVariableLength(input, 3);
                    length += 2;

                    var sign = codeByte & 0x8;
                    codeByte = (byte)input.ReadByte();
                    var byte2 = (byte)input.ReadByte();

                    if (sign == 0 && codeByte >> 2 == 0 && byte2 == 0)
                        // End of decompression
                        break;

                    var displacement = (codeByte >> 2) + (byte2 << 6) + (sign > 0 ? 0x4000 : 0) + 0x4000;

                    circularBuffer.Copy(output, displacement, length);
                }
                else
                {
                    // 10 bit displacement
                    // Min disp: 1; Max disp: 0x400
                    // Min length: 2; Max length: 2

                    var byte2 = (byte)input.ReadByte();

                    var displacement = ((byte2 << 2) | (codeByte >> 2)) + 1;

                    circularBuffer.Copy(output, displacement, 2);
                }

                SubLoop(input, output, circularBuffer, codeByte);
            }
        }

        private void SubLoop(Stream input, Stream output, CircularBuffer circularBuffer, byte codeByte)
        {
            while (true)
            {
                if ((codeByte & 0x3) != 0)
                {
                    ReadRawData(input, output, circularBuffer, codeByte & 0x3);
                    break;
                }

                if (ReadSubLoopRawData(input, output, circularBuffer))
                    break;

                if (ReadSubLoopMatch(input, output, circularBuffer, out codeByte))
                    break;
            }
        }

        private bool ReadSubLoopRawData(Stream input, Stream output, CircularBuffer circularBuffer)
        {
            // Raw Data read
            var codeByte = (byte)input.ReadByte();
            if (codeByte > 0xF)
            {
                input.Position--;
                return true;
            }

            var length = codeByte + 3;
            if (codeByte == 0)
                length += ReadVariableLength(input, 4);
            ReadRawData(input, output, circularBuffer, length);

            return false;
        }

        private bool ReadSubLoopMatch(Stream input, Stream output, CircularBuffer circularBuffer, out byte codeByte)
        {
            // Read LZ match
            codeByte = (byte)input.ReadByte();
            if (codeByte > 0xF)
            {
                input.Position--;
                return true;
            }

            var codeByte2 = (byte)input.ReadByte();
            var displacement = ((codeByte2 << 2) | (codeByte >> 2)) + 0x801;

            circularBuffer.Copy(output, displacement, 3);

            return false;
        }

        private void ReadRawData(Stream input, Stream output, CircularBuffer circularBuffer, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var value = (byte)input.ReadByte();

                output.WriteByte(value);
                circularBuffer.WriteByte(value);
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
