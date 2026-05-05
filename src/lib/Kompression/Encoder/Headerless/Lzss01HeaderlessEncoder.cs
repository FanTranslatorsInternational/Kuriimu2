using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder.Headerless
{
    public class Lzss01HeaderlessEncoder : ILempelZivEncoder
    {
        private const int WindowBufferLength_ = 0x1000;
        private const int PreBufferSize_ = 0xFEE;

        internal class Block
        {
            public readonly byte[] Buffer = new byte[1 + 8 * 2];
            public int BufferLength = 1;
            public int FlagCount;
        }

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new Lzss01PriceCalculator())
                .FindPatternMatches().WithinLimitations(3, 0x12, 1, 0x1000)
                .AdjustInput(input => input.Prepend(PreBufferSize_));
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var block = new Block();

            foreach (var match in matches)
            {
                if (input.Position < match.Position)
                    WriteRawData(input, output, block, match.Position - input.Position);

                WriteMatchData(input, output, block, match);
            }

            if (input.Position < input.Length)
                WriteRawData(input, output, block, input.Length - input.Position);

            WriteAndResetBuffer(output, block);
        }

        private static void WriteRawData(Stream input, Stream output, Block block, long rawLength)
        {
            for (var i = 0; i < rawLength; i++)
            {
                if (block.FlagCount == 8)
                    WriteAndResetBuffer(output, block);

                block.Buffer[0] |= (byte)(1 << block.FlagCount++);
                block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
            }
        }

        private static void WriteMatchData(Stream input, Stream output, Block block, LempelZivMatch lempelZivMatch)
        {
            if (block.FlagCount == 8)
                WriteAndResetBuffer(output, block);

            var bufferPosition = (PreBufferSize_ + lempelZivMatch.Position - lempelZivMatch.Displacement) % WindowBufferLength_;

            var byte2 = (byte)(lempelZivMatch.Length - 3 & 0xF);
            byte2 |= (byte)(bufferPosition >> 4 & 0xF0);
            var byte1 = (byte)bufferPosition;

            block.FlagCount++;
            block.Buffer[block.BufferLength++] = byte1;
            block.Buffer[block.BufferLength++] = byte2;
            input.Position += lempelZivMatch.Length;
        }

        private static void WriteAndResetBuffer(Stream output, Block block)
        {
            output.Write(block.Buffer, 0, block.BufferLength);

            Array.Clear(block.Buffer, 0, block.BufferLength);
            block.BufferLength = 1;
            block.FlagCount = 0;
        }
    }
}
