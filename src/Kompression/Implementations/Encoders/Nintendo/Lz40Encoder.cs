using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch.MatchFinders;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders.Nintendo
{
    public class Lz40Encoder : ILzEncoder
    {
        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new Lz40PriceCalculator())
                .FindWith((options, limits) => new HistoryMatchFinder(limits, options))
                .WithinLimitations(() => new FindLimitations(0x3, 0x1010F, 1, 0xFFF));
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x40, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            WriteCompressedData(input, output, matches.ToArray());
        }

        internal void WriteCompressedData(Stream input, Stream output, Match[] matches)
        {
            int bufferedBlocks = 0, blockBufferLength = 1, lzIndex = 0;
            byte[] blockBuffer = new byte[8 * 4 + 1];

            while (input.Position < input.Length)
            {
                if (bufferedBlocks >= 8)
                {
                    WriteBlockBuffer(output, blockBuffer, blockBufferLength);

                    bufferedBlocks = 0;
                    blockBufferLength = 1;
                }

                if (lzIndex < matches.Length && input.Position == matches[lzIndex].Position)
                {
                    blockBufferLength = WriteCompressedBlockToBuffer(matches[lzIndex], blockBuffer, blockBufferLength, bufferedBlocks);
                    input.Position += matches[lzIndex++].Length;
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
            // mark the next block as compressed
            blockBuffer[0] |= (byte)(1 << (7 - bufferedBlocks));

            // the last 1.5 bytes are always the displacement
            blockBuffer[blockBufferLength] = (byte)((lzMatch.Displacement & 0x0F) << 4);
            blockBuffer[blockBufferLength + 1] = (byte)((lzMatch.Displacement >> 4) & 0xFF);

            if (lzMatch.Length > 0x10F)
            {
                // case 1: (A)1 (CD) (EF GH) + (0x0)(0x110) = (DISP = A-C-D)(LEN = E-F-G-H)
                blockBuffer[blockBufferLength] |= 0x01;
                blockBufferLength += 2;
                blockBuffer[blockBufferLength++] = (byte)((lzMatch.Length - 0x110) & 0xFF);
                blockBuffer[blockBufferLength] = (byte)(((lzMatch.Length - 0x110) >> 8) & 0xFF);
            }
            else if (lzMatch.Length > 0xF)
            {
                // case 0; (A)0 (CD) (EF) + (0x0)(0x10) = (DISP = A-C-D)(LEN = E-F)
                blockBuffer[blockBufferLength] |= 0x00;
                blockBufferLength += 2;
                blockBuffer[blockBufferLength] = (byte)((lzMatch.Length - 0x10) & 0xFF);
            }
            else
            {
                // case > 1: (A)(B) (CD) + (0x0)(0x0) = (DISP = A-C-D)(LEN = B)
                blockBuffer[blockBufferLength++] |= (byte)(lzMatch.Length & 0x0F);
            }

            blockBufferLength++;
            return blockBufferLength;
        }

        private void WriteBlockBuffer(Stream output, byte[] blockBuffer, int blockBufferLength)
        {
            output.Write(blockBuffer, 0, blockBufferLength);
            Array.Clear(blockBuffer, 0, blockBufferLength);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
