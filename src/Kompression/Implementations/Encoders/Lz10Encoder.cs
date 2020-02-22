using System;
using System.IO;
using System.Linq;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    public class Lz10Encoder : IEncoder
    {
        private readonly IMatchParser _matchParser;

        public Lz10Encoder(IMatchParser matchParser)
        {
            _matchParser = matchParser;
        }

        public void Encode(Stream input, Stream output)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x10, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            WriteCompressedData(input, output);
        }

        internal void WriteCompressedData(Stream input, Stream output)
        {
            var matches = _matchParser.ParseMatches(input).ToArray();

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

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
