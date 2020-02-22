using System;
using System.IO;
using Kompression.Extensions;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    // TODO: Refactor block class
    public class TalesOf01Encoder : IEncoder
    {
        private const int WindowBufferLength = 0x1000;

        private IMatchParser _matchParser;

        class Block
        {
            public byte[] buffer = new byte[1 + 8 * 2];
            public int bufferLength = 1;
            public int flagCount;
        }

        public TalesOf01Encoder(IMatchParser parser)
        {
            _matchParser = parser;
        }

        public void Encode(Stream input, Stream output)
        {
            var block = new Block();
            output.Position += 9;

            var matches = _matchParser.ParseMatches(input);
            foreach (var match in matches)
            {
                if (input.Position < match.Position - _matchParser.FindOptions.PreBufferSize)
                    WriteRawData(input, output, block, match.Position - _matchParser.FindOptions.PreBufferSize - input.Position);

                WriteMatchData(input, output, block, match);
            }

            if (input.Position < input.Length)
                WriteRawData(input, output, block, input.Length - input.Position);

            WriteAndResetBuffer(output, block);

            WriteHeaderData(output, (int)input.Length);
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

            var bufferPosition = (match.Position - match.Displacement) % WindowBufferLength;

            var byte2 = (byte)((match.Length - 3) & 0xF);
            byte2 |= (byte)((bufferPosition >> 4) & 0xF0);
            var byte1 = (byte)bufferPosition;

            block.flagCount++;
            block.buffer[block.bufferLength++] = byte1;
            block.buffer[block.bufferLength++] = byte2;
            input.Position += match.Length;
        }

        private void WriteHeaderData(Stream output, int decompressedLength)
        {
            var endPosition = output.Position;
            output.Position = 0;

            output.WriteByte(1);
            Write(output, ((int)output.Length).GetArrayLittleEndian());
            Write(output, decompressedLength.GetArrayLittleEndian());

            output.Position = endPosition;
        }

        private void WriteAndResetBuffer(Stream output, Block block)
        {
            output.Write(block.buffer, 0, block.bufferLength);

            Array.Clear(block.buffer, 0, block.bufferLength);
            block.bufferLength = 1;
            block.flagCount = 0;
        }

        private void Write(Stream output, byte[] data)
        {
#if NET_CORE_31
            output.Write(data);
#else
            output.Write(data, 0, data.Length);
#endif
        }

        public void Dispose()
        {
            _matchParser?.Dispose();
            _matchParser = null;
        }
    }
}
