using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch.MatchFinders;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders.Headerless
{
    public class IrLzHeaderlessEncoder : ILzEncoder
    {
        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new IrLzPriceCalculator())
                .FindWith((options, limits) => new HistoryMatchFinder(limits, options))
                .WithinLimitations(() => new FindLimitations(2, 0x11, 1, 0x1000));
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
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

        private int WriteCompressedBlockToBuffer(Match lzMatch, byte[] blockBuffer, int blockBufferLength, int bufferedBlocks)
        {
            blockBuffer[0] |= (byte)(1 << bufferedBlocks);

            blockBuffer[blockBufferLength++] = (byte)((lzMatch.Displacement - 1) & 0xFF);
            blockBuffer[blockBufferLength] = (byte)(((lzMatch.Displacement - 1) >> 8) & 0x0F);
            blockBuffer[blockBufferLength++] |= (byte)(((lzMatch.Length - 2) & 0x0F) << 4);

            return blockBufferLength;
        }

        private void WriteBlockBuffer(Stream output, byte[] blockBuffer, int blockBufferLength)
        {
            output.Write(blockBuffer, 0, blockBufferLength);
            Array.Clear(blockBuffer, 0, blockBufferLength);
        }

        public void Dispose()
        {
        }
    }
}
