using System.IO;
using Kompression.IO;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders.Headerless
{
    public class SpikeChunsoftHeaderlessDecoder : IDecoder
    {
        public static long CalculateDecompressedSize(Stream input)
        {
            var decompressedSize = 0L;
            while (input.Position < input.Length)
            {
                var flag = input.ReadByte();

                if ((flag & 0x80) == 0x80)
                {
                    // Lz match start
                    decompressedSize += ((flag >> 5) & 0x3) + 4;
                    input.Position++;
                }
                else if ((flag & 0x60) == 0x60)
                {
                    // Lz match continue
                    decompressedSize += flag & 0x1F;
                }
                else if ((flag & 0x40) == 0x40)
                {
                    // Rle data
                    int length;
                    if ((flag & 0x10) == 0x00)
                        length = (flag & 0xF) + 4;
                    else
                        length = ((flag & 0xF) << 8) + input.ReadByte() + 4;

                    input.Position++;
                    decompressedSize += length;
                }
                else
                {
                    // Raw data
                    int length;
                    if ((flag & 0x20) == 0x00)
                        length = flag & 0x1F;
                    else
                        length = ((flag & 0x1F) << 8) + input.ReadByte();

                    input.Position += length;
                    decompressedSize += length;
                }
            }

            return decompressedSize;
        }

        public void Decode(Stream input, Stream output)
        {
            var circularBuffer = new CircularBuffer(0x1FFF);

            int flag;
            var displacement = 0;
            while ((flag = input.ReadByte()) != -1)
            {
                var flagByte = (byte)flag;

                if ((flag & 0x80) == 0x80)
                {
                    // Lz match start
                    displacement = ReadMatchStart(input, output, circularBuffer, flagByte);
                }
                else if ((flag & 0x60) == 0x60)
                {
                    // Lz match continue
                    ReadMatchContinue(output, circularBuffer, flagByte, displacement);
                }
                else if ((flag & 0x40) == 0x40)
                {
                    // Rle data
                    ReadRle(input, output, circularBuffer, flagByte);
                }
                else
                {
                    // Raw data
                    ReadRawData(input, output, circularBuffer, flagByte);
                }
            }
        }

        public void Decode(Stream input, Stream output, int decompressedSize)
        {
            var circularBuffer = new CircularBuffer(0x1FFF);

            var displacement = 0;
            while (output.Position < decompressedSize)
            {
                var flag = (byte)input.ReadByte();

                if ((flag & 0x80) == 0x80)
                {
                    // Lz match start
                    displacement = ReadMatchStart(input, output, circularBuffer, flag);
                }
                else if ((flag & 0x60) == 0x60)
                {
                    // Lz match continue
                    ReadMatchContinue(output, circularBuffer, flag, displacement);
                }
                else if ((flag & 0x40) == 0x40)
                {
                    // Rle data
                    ReadRle(input, output, circularBuffer, flag);
                }
                else
                {
                    // Raw data
                    ReadRawData(input, output, circularBuffer, flag);
                }
            }
        }

        private int ReadMatchStart(Stream input, Stream output, CircularBuffer circularBuffer, byte flag)
        {
            // Min length: 4, Max length: 7
            // Min disp: 0, Max disp: 0x1FFF

            var length = ((flag >> 5) & 0x3) + 4;
            var displacement = (flag & 0x1F) << 8;
            displacement |= input.ReadByte();

            circularBuffer.Copy(output, displacement, length);

            return displacement;
        }

        private void ReadMatchContinue(Stream output, CircularBuffer circularBuffer, byte flag, int previousDisplacement)
        {
            // Min length: 0, Max length: 0x1F

            var length = flag & 0x1F;

            circularBuffer.Copy(output, previousDisplacement, length);
        }

        private void ReadRle(Stream input, Stream output, CircularBuffer circularBuffer, byte flag)
        {
            // Min length: 4, Max length: 0x1003

            int length;
            if ((flag & 0x10) == 0x00)
                length = (flag & 0xF) + 4;
            else
                length = ((flag & 0xF) << 8) + input.ReadByte() + 4;

            var value = (byte)input.ReadByte();

            for (var i = 0; i < length; i++)
            {
                output.WriteByte(value);
                circularBuffer.WriteByte(value);
            }
        }

        private void ReadRawData(Stream input, Stream output, CircularBuffer circularBuffer, byte flag)
        {
            // Min length: 0, Max length: 0x1FFF

            int length;
            if ((flag & 0x20) == 0x00)
                length = flag & 0x1F;
            else
                length = ((flag & 0x1F) << 8) + input.ReadByte();

            for (var i = 0; i < length; i++)
            {
                var nextValue = (byte)input.ReadByte();

                output.WriteByte(nextValue);
                circularBuffer.WriteByte(nextValue);
            }
        }

        public void Dispose()
        {
        }
    }
}
