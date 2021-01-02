using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders.Headerless
{
    // TODO: Check all compressions for matches.ToArray() and if it's necessary
    public class Lz10HeaderlessEncoder
    {
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
            blockBuffer[0] |= (byte)(1 << (7 - bufferedBlocks));

            blockBuffer[blockBufferLength] = (byte)(((lzMatch.Length - 3) << 4) & 0xF0);
            blockBuffer[blockBufferLength++] |= (byte)(((lzMatch.Displacement - 1) >> 8) & 0x0F);
            blockBuffer[blockBufferLength++] = (byte)((lzMatch.Displacement - 1) & 0xFF);

            return blockBufferLength;
        }

        private void WriteBlockBuffer(Stream output, byte[] blockBuffer, int blockBufferLength)
        {
            output.Write(blockBuffer, 0, blockBufferLength);
            Array.Clear(blockBuffer, 0, blockBufferLength);
        }
    }
}
