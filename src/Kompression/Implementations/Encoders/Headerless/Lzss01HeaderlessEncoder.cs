using System;
using System.Collections.Generic;
using System.IO;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch.MatchFinders;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders.Headerless
{
    // TODO: Refactor block class
    public class Lzss01HeaderlessEncoder : ILzEncoder
    {
        private const int WindowBufferLength_ = 0x1000;
        private const int PreBufferSize_ = 0xFEE;

        class Block
        {
            public byte[] buffer = new byte[1 + 8 * 2];
            public int bufferLength = 1;
            public int flagCount;
        }

        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new Lzss01PriceCalculator())
                .FindWith((options, limits) => new HistoryMatchFinder(limits, options))
                .WithinLimitations(() => new FindLimitations(3, 0x12, 1, 0x1000))
                .AdjustInput(input => input.Prepend(PreBufferSize_));
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
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

        private void WriteRawData(Stream input, Stream output, Block block, long rawLength)
        {
            for (var i = 0; i < rawLength; i++)
            {
                if (block.flagCount == 8)
                    WriteAndResetBuffer(output, block);

                block.buffer[0] |= (byte)(1 << block.flagCount++);
                block.buffer[block.bufferLength++] = (byte)input.ReadByte();
            }
        }

        private void WriteMatchData(Stream input, Stream output, Block block, Match match)
        {
            if (block.flagCount == 8)
                WriteAndResetBuffer(output, block);

            var bufferPosition = (PreBufferSize_ + match.Position - match.Displacement) % WindowBufferLength_;

            var byte2 = (byte)((match.Length - 3) & 0xF);
            byte2 |= (byte)((bufferPosition >> 4) & 0xF0);
            var byte1 = (byte)bufferPosition;

            block.flagCount++;
            block.buffer[block.bufferLength++] = byte1;
            block.buffer[block.bufferLength++] = byte2;
            input.Position += match.Length;
        }

        private void WriteAndResetBuffer(Stream output, Block block)
        {
            output.Write(block.buffer, 0, block.bufferLength);

            Array.Clear(block.buffer, 0, block.bufferLength);
            block.bufferLength = 1;
            block.flagCount = 0;
        }

        public void Dispose()
        {
        }
    }
}
