using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kompression.Extensions;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    // TODO: Refactor block class
    class Wp16Encoder : ILzEncoder
    {
        class Block
        {
            public long flagBuffer;
            public int flagPosition;

            // at max 32 matches, one match is 2 bytes
            public byte[] buffer = new byte[32 * 2];
            public int bufferLength;
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            var block = new Block();

            var start = Encoding.ASCII.GetBytes("Wp16");
            output.Write(start, 0, 4);
            output.Write(((int)input.Length).GetArrayLittleEndian(), 0, 4);

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

            if (block.flagPosition > 0)
                WriteAndResetBuffer(output, block);
        }

        private void CompressRawData(Stream input, Stream output, Block block, int rawLength)
        {
            while (rawLength > 0)
            {
                if (block.flagPosition == 32)
                    WriteAndResetBuffer(output, block);

                rawLength -= 2;
                block.flagBuffer |= 1L << block.flagPosition++;

                block.buffer[block.bufferLength++] = (byte)input.ReadByte();
                block.buffer[block.bufferLength++] = (byte)input.ReadByte();
            }

            if (block.flagPosition == 32)
                WriteAndResetBuffer(output, block);
        }

        private void CompressMatchData(Stream input, Stream output, Block block, Match match)
        {
            if (block.flagPosition == 32)
                WriteAndResetBuffer(output, block);

            block.flagPosition++;

            var byte1 = (byte)((match.Length / 2 - 2) & 0x1F);
            byte1 |= (byte)(((match.Displacement / 2) & 0x7) << 5);
            var byte2 = (byte)((match.Displacement / 2) >> 3);

            block.buffer[block.bufferLength++] = byte1;
            block.buffer[block.bufferLength++] = byte2;

            if (block.flagPosition == 32)
                WriteAndResetBuffer(output, block);

            input.Position += match.Length;
        }

        private void WriteAndResetBuffer(Stream output, Block block)
        {
            // Write data to output
            var buffer = ((int)block.flagBuffer).GetArrayLittleEndian();
            output.Write(buffer, 0, 4);
            output.Write(block.buffer, 0, block.bufferLength);

            // Reset codeBlock and buffer
            block.flagBuffer = 0;
            block.flagPosition = 0;
            Array.Clear(block.buffer, 0, block.bufferLength);
            block.bufferLength = 0;
        }

        public void Dispose()
        {
        }
    }
}
