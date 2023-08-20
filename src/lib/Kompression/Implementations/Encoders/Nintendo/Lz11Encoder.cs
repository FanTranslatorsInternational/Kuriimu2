﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Kompression.Implementations.PriceCalculators;
using Kontract.Kompression.Interfaces.Configuration;
using Kontract.Kompression.Models.PatternMatch;

namespace Kompression.Implementations.Encoders.Nintendo
{
    public class Lz11Encoder : ILzEncoder
    {
        public void Configure(IInternalMatchOptions matchOptions)
        {
	        matchOptions.CalculatePricesWith(() => new Lz11PriceCalculator())
		        .FindMatches().WithinLimitations(3, 0x10110, 1, 0x1000);
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x11, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            WriteCompressedData(input, output, matches.ToArray());
        }

        private void WriteCompressedData(Stream input, Stream output, Match[] matches)
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

            if (lzMatch.Length > 0x110)
            {
                // case 1: 1(B CD E)(F GH) + (0x111)(0x1) = (LEN)(DISP)
                blockBuffer[blockBufferLength] = 0x10;
                blockBuffer[blockBufferLength++] |= (byte)(((lzMatch.Length - 0x111) >> 12) & 0x0F);
                blockBuffer[blockBufferLength++] = (byte)(((lzMatch.Length - 0x111) >> 4) & 0xFF);
                blockBuffer[blockBufferLength] = (byte)(((lzMatch.Length - 0x111) << 4) & 0xF0);
            }
            else if (lzMatch.Length > 0x10)
            {
                // case 0; 0(B C)(D EF) + (0x11)(0x1) = (LEN)(DISP)
                blockBuffer[blockBufferLength] = 0x00;
                blockBuffer[blockBufferLength++] |= (byte)(((lzMatch.Length - 0x11) >> 4) & 0x0F);
                blockBuffer[blockBufferLength] = (byte)(((lzMatch.Length - 0x11) << 4) & 0xF0);
            }
            else
            {
                // case > 1: (A)(B CD) + (0x1)(0x1) = (LEN)(DISP)
                blockBuffer[blockBufferLength] = (byte)(((lzMatch.Length - 1) << 4) & 0xF0);
            }

            // the last 1.5 bytes are always the disp
            blockBuffer[blockBufferLength++] |= (byte)(((lzMatch.Displacement - 1) >> 8) & 0x0F);
            blockBuffer[blockBufferLength++] = (byte)((lzMatch.Displacement - 1) & 0xFF);

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
