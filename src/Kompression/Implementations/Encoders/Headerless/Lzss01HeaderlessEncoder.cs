using System;
using System.IO;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders.Headerless
{
    // TODO: Refactor block class
    public class Lzss01HeaderlessEncoder : IEncoder
    {
        private const int WindowBufferLength = 0x1000;

        private IMatchParser _matchParser;

        class Block
        {
            public byte[] buffer = new byte[1 + 8 * 2];
            public int bufferLength = 1;
            public int flagCount;
        }

        public Lzss01HeaderlessEncoder(IMatchParser matchParser)
        {
            _matchParser = matchParser;
        }

        public void Encode(Stream input, Stream output)
        {
            var block = new Block();

            var matches = _matchParser.ParseMatches(input);
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

            var bufferPosition = (_matchParser.FindOptions.PreBufferSize + match.Position - match.Displacement) % WindowBufferLength;

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
