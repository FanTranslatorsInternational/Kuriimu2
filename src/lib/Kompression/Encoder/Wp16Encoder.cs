using Komponent.IO;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Contract.Enums.Encoder.LempelZiv;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder
{
    internal class Wp16Encoder : ILempelZivEncoder
    {
        private const int PreBufferSize_ = 0xFFE;

        internal class Block
        {
            public long FlagBuffer;
            public int FlagPosition;

            // at max 32 matches, one match is 2 bytes
            public readonly byte[] Buffer = new byte[32 * 2];
            public int BufferLength;
        }

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new Wp16PriceCalculator())
                .FindPatternMatches().WithinLimitations(4, 0x42, 2, 0xFFE)
                .AdjustInput(input => input.Prepend(PreBufferSize_))
                .HasUnitSize(UnitSize.Short);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var block = new Block();

            using var bw = new BinaryWriterX(output, true);

            bw.WriteString("Wp16", writeNullTerminator: false);
            bw.Write((int)input.Length);

            foreach (var match in matches)
            {
                // Compress raw data
                if (input.Position < match.Position)
                    CompressRawData(input, output, block, (int)(match.Position - input.Position));

                // Compress match
                CompressMatchData(input, output, block, match);
            }

            // Compress raw data
            if (input.Position < input.Length)
                CompressRawData(input, output, block, (int)(input.Length - input.Position));

            if (block.FlagPosition > 0)
                WriteAndResetBuffer(output, block);
        }

        private static void CompressRawData(Stream input, Stream output, Block block, int rawLength)
        {
            while (rawLength > 0)
            {
                if (block.FlagPosition == 32)
                    WriteAndResetBuffer(output, block);

                rawLength -= 2;
                block.FlagBuffer |= 1L << block.FlagPosition++;

                block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
                block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
            }

            if (block.FlagPosition == 32)
                WriteAndResetBuffer(output, block);
        }

        private static void CompressMatchData(Stream input, Stream output, Block block, LempelZivMatch lempelZivMatch)
        {
            if (block.FlagPosition == 32)
                WriteAndResetBuffer(output, block);

            block.FlagPosition++;

            var byte1 = (byte)(lempelZivMatch.Length / 2 - 2 & 0x1F);
            byte1 |= (byte)((lempelZivMatch.Displacement / 2 & 0x7) << 5);
            var byte2 = (byte)(lempelZivMatch.Displacement / 2 >> 3);

            block.Buffer[block.BufferLength++] = byte1;
            block.Buffer[block.BufferLength++] = byte2;

            if (block.FlagPosition == 32)
                WriteAndResetBuffer(output, block);

            input.Position += lempelZivMatch.Length;
        }

        private static void WriteAndResetBuffer(Stream output, Block block)
        {
            using var bw = new BinaryWriterX(output, true);

            // Write data to output
            bw.Write((int)block.FlagBuffer);
            output.Write(block.Buffer, 0, block.BufferLength);

            // Reset codeBlock and buffer
            block.FlagBuffer = 0;
            block.FlagPosition = 0;
            Array.Clear(block.Buffer, 0, block.BufferLength);
            block.BufferLength = 0;
        }
    }
}
