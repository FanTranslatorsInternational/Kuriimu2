using System;
using System.Collections.Generic;
using System.IO;

using Kompression.Implementations.PriceCalculators;
using Kontract.Kompression.Interfaces.Configuration;
using Kontract.Kompression.Models.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    // TODO: Refactor block class
    public class Dr3Encoder : ILzEncoder
    {
        class Block
        {
            public byte codeBlock;
            public int codeBlockPosition = 8;

            // each buffer can be at max 8 pairs of compressed matches; a compressed match is 2 bytes
            public byte[] buffer = new byte[8 * 2];
            public int bufferLength;
        }

        private const int WindowBufferLength_ = 0x400;
        private const int PreBufferSize_ = 0x3FA;

        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new Dr3PriceCalculator())
                .FindMatches().WithinLimitations(2, 0x41, 1, WindowBufferLength_)
                .AdjustInput(config => config.Prepend(PreBufferSize_));
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            var block = new Block();

            foreach (var match in matches)
            {
                // Write any data before the match, to the uncompressed table
                while (input.Position < match.Position)
                {
                    if (block.codeBlockPosition == 0)
                        WriteAndResetBuffer(output, block);

                    block.codeBlock |= (byte)(1 << --block.codeBlockPosition);
                    block.buffer[block.bufferLength++] = (byte)input.ReadByte();
                }

                // Write match data to the buffer
                var bufferPosition = WindowBufferLength_ - match.Displacement;
                var firstByte = (byte)bufferPosition;
                var secondByte = (byte)((bufferPosition >> 8) | (byte)((match.Length - 2) << 2));

                if (block.codeBlockPosition == 0)
                    WriteAndResetBuffer(output, block);

                block.codeBlockPosition--; // Since a match is flagged with a 0 bit, we don't need a bit shift and just decrease the position
                block.buffer[block.bufferLength++] = firstByte;
                block.buffer[block.bufferLength++] = secondByte;

                input.Position += match.Length;
            }

            // Write any data after last match, to the buffer
            while (input.Position < input.Length)
            {
                if (block.codeBlockPosition == 0)
                    WriteAndResetBuffer(output, block);

                block.codeBlock |= (byte)(1 << --block.codeBlockPosition);
                block.buffer[block.bufferLength++] = (byte)input.ReadByte();
            }

            // Flush remaining buffer to stream
            if (block.codeBlockPosition > 0)
                WriteAndResetBuffer(output, block);
        }

        private void WriteAndResetBuffer(Stream output, Block block)
        {
            // Write data to output
            output.WriteByte(block.codeBlock);
            output.Write(block.buffer, 0, block.bufferLength);

            // Reset codeBlock and buffer
            block.codeBlock = 0;
            block.codeBlockPosition = 8;
            Array.Clear(block.buffer, 0, block.bufferLength);
            block.bufferLength = 0;
        }
    }
}
