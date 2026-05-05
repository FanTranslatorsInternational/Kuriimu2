using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder.Headerless
{
    public class IrLzHeaderlessEncoder : ILempelZivEncoder
    {
        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new IrLzPriceCalculator())
                .FindPatternMatches().WithinLimitations(2, 0x11, 1, 0x1000);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var matchArray = matches.ToArray();

            int bufferedBlocks = 0, blockBufferLength = 1, lzIndex = 0;
            byte[] blockBuffer = new byte[8 * 2 + 1];

            while (input.Position < input.Length)
            {
                if (bufferedBlocks >= 8)
                {
                    WriteBlockBuffer(output, blockBuffer, blockBufferLength);

                    bufferedBlocks = 0;
                    blockBufferLength = 1;
                }

                if (lzIndex < matchArray.Length && input.Position == matchArray[lzIndex].Position)
                {
                    blockBufferLength = WriteCompressedBlockToBuffer(matchArray[lzIndex], blockBuffer, blockBufferLength, bufferedBlocks);
                    input.Position += matchArray[lzIndex++].Length;
                }
                else
                {
                    blockBuffer[blockBufferLength++] = (byte)input.ReadByte();
                }

                bufferedBlocks++;
            }

            WriteBlockBuffer(output, blockBuffer, blockBufferLength);
        }

        private static int WriteCompressedBlockToBuffer(LempelZivMatch lzLempelZivMatch, byte[] blockBuffer, int blockBufferLength, int bufferedBlocks)
        {
            blockBuffer[0] |= (byte)(1 << bufferedBlocks);

            blockBuffer[blockBufferLength++] = (byte)(lzLempelZivMatch.Displacement - 1 & 0xFF);
            blockBuffer[blockBufferLength] = (byte)(lzLempelZivMatch.Displacement - 1 >> 8 & 0x0F);
            blockBuffer[blockBufferLength++] |= (byte)((lzLempelZivMatch.Length - 2 & 0x0F) << 4);

            return blockBufferLength;
        }

        private static void WriteBlockBuffer(Stream output, byte[] blockBuffer, int blockBufferLength)
        {
            output.Write(blockBuffer, 0, blockBufferLength);
            Array.Clear(blockBuffer, 0, blockBufferLength);
        }
    }
}
