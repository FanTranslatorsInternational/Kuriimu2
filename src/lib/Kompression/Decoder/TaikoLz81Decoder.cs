using Komponent.Contract.Enums;
using Komponent.IO;
using Kompression.Contract.Decoder;
using Kompression.Decoder.TaikoLz81;
using Kompression.IO;

namespace Kompression.Decoder
{
    public class TaikoLz81Decoder : IDecoder
    {
        private static readonly int[] Counters =
        [
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 0xa, 0xc, 0xe,
            0x10, 0x12, 0x16, 0x1a,
            0x1e, 0x22, 0x2a, 0x32,
            0x3a, 0x42, 0x52, 0x62,
            0x72, 0x82, 0xa2, 0xc2,
            0xe2, 0x102, 0, 0
        ];

        private static readonly int[] CounterBitReads =
        [
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 1, 1, 1,
            1, 2, 2, 2,
            2, 3, 3, 3,
            3, 4, 4, 4,
            4, 5, 5, 5,
            5, 0, 0, 0
        ];

        private static readonly int[] DispRanges =
        [
            1, 2, 3, 4,
            5, 7, 9, 0xd,
            0x11, 0x19, 0x21, 0x31,
            0x41, 0x61, 0x81, 0xc1,
            0x101, 0x181, 0x201, 0x301,
            0x401, 0x601, 0x801, 0xc01,
            0x1001, 0x1801, 0x2001, 0x3001,
            0x4001, 0x6001, 0, 0
        ];

        private static readonly int[] DispBitReads =
        [
            0, 0, 0, 0,
            1, 1, 2, 2,
            3, 3, 4, 4,
            5, 5, 6, 6,
            7, 7, 8, 8,
            9, 9, 0xa, 0xa,
            0xb, 0xb, 0xc, 0xc,
            0xd, 0xd, 0, 0
        ];

        public void Decode(Stream input, Stream output)
        {
            var circularBuffer = new CircularBuffer(0x8000);

            using var br = new BinaryBitReader(input, BitOrder.LeastSignificantBitFirst, 1, ByteOrder.LittleEndian);
            var initialByte = br.ReadBits<int>(8);

            // 3 value mappings
            var rawValueMapping = new TaikoLz81Tree();
            rawValueMapping.Build(br, 8);
            var indexValueMapping = new TaikoLz81Tree();
            indexValueMapping.Build(br, 6);
            var dispIndexMapping = new TaikoLz81Tree();
            dispIndexMapping.Build(br, 5);

            while (true)
            {
                var index = indexValueMapping.ReadValue(br);

                if (index < 0x20)
                {
                    if (index == 0)
                    {
                        DeobfuscateData(output, initialByte);
                        break;
                    }

                    // Match reading
                    ReadCompressedData(br, output, circularBuffer, dispIndexMapping, index);
                }
                else
                {
                    // Raw data reading
                    ReadUncompressedData(br, output, circularBuffer, rawValueMapping, index - 0x20);
                }
            }
        }

        private static void ReadUncompressedData(BinaryBitReader br, Stream output, CircularBuffer circularBuffer, TaikoLz81Tree rawValueMapping, int index)
        {
            var counter = Counters[index];
            if (CounterBitReads[index] != 0)
                counter += br.ReadBits<int>(CounterBitReads[index]);

            if (counter == 0)
                return;

            for (int i = 0; i < counter; i++)
            {
                var rawValue = (byte)rawValueMapping.ReadValue(br);

                output.WriteByte(rawValue);
                circularBuffer.WriteByte(rawValue);
            }
        }

        private static void ReadCompressedData(BinaryBitReader br, Stream output, CircularBuffer circularBuffer, TaikoLz81Tree dispIndexMapping, int index)
        {
            // Max displacement 0x8000; Min displacement 2
            // Max length 0x102; Min length 1
            var length = Counters[index];
            if (CounterBitReads[index] != 0)
                length += br.ReadBits<int>(CounterBitReads[index]);

            var dispIndex = dispIndexMapping.ReadValue(br);

            var displacement = DispRanges[dispIndex];
            if (DispBitReads[dispIndex] != 0)
                displacement += br.ReadBits<int>(DispBitReads[dispIndex]);

            if (length == 0)
                return;

            circularBuffer.Copy(output, displacement, length);
        }

        private static void DeobfuscateData(Stream output, int initialByte)
        {
            if (initialByte < 3)
                return;

            var iVar4 = initialByte - 2;
            if (output.Length <= iVar4)
                return;

            var length = output.Length - iVar4;
            var position = 0;
            do
            {
                length--;

                output.Position = position;
                var byte1 = output.ReadByte();

                output.Position = position + iVar4;
                var byte2 = output.ReadByte();

                output.Position--;
                output.WriteByte((byte)(byte1 + byte2));

                position++;
            } while (length != 0);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
