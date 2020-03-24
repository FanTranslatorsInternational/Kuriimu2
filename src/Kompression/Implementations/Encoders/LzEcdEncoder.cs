using System;
using System.IO;
using System.Text;
using Kompression.Extensions;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Encoders
{
    // TODO: Refactor block class
    public class LzEcdEncoder : IEncoder
    {
        class Block
        {
            public byte codeBlock;
            public int codeBlockPosition = 0;

            // each buffer can be at max 8 pairs of compressed matches; a compressed match is 2 bytes
            public byte[] buffer = new byte[8 * 2];
            public int bufferLength;
        }

        private const int WindowBufferLength = 0x400;
        private readonly IMatchParser _matchParser;

        public LzEcdEncoder(IMatchParser matchParser)
        {
            _matchParser = matchParser;
        }

        public void Encode(Stream input, Stream output)
        {
            var originalOutputPosition = output.Position;
            output.Position += 0x10;

            var block = new Block();

            var matches = _matchParser.ParseMatches(input);
            foreach (var match in matches)
            {
                // Write any data before the match, to the uncompressed table
                while (input.Position < match.Position)
                {
                    if (block.codeBlockPosition == 8)
                        WriteAndResetBuffer(output, block);

                    block.codeBlock |= (byte)(1 << block.codeBlockPosition++);
                    block.buffer[block.bufferLength++] = (byte)input.ReadByte();
                }

                // Write match data to the buffer
                var bufferPosition = (_matchParser.FindOptions.PreBufferSize + match.Position - match.Displacement) % WindowBufferLength;
                var firstByte = (byte)bufferPosition;
                var secondByte = (byte)(((bufferPosition >> 2) & 0xC0) | (byte)(match.Length - 3));

                if (block.codeBlockPosition == 8)
                    WriteAndResetBuffer(output, block);

                block.codeBlockPosition++; // Since a match is flagged with a 0 bit, we don't need a bit shift and just increase the position
                block.buffer[block.bufferLength++] = firstByte;
                block.buffer[block.bufferLength++] = secondByte;

                input.Position += match.Length;
            }

            // Write any data after last match, to the buffer
            while (input.Position < input.Length)
            {
                if (block.codeBlockPosition == 8)
                    WriteAndResetBuffer(output, block);

                block.codeBlock |= (byte)(1 << block.codeBlockPosition++);
                block.buffer[block.bufferLength++] = (byte)input.ReadByte();
            }

            // Flush remaining buffer to stream
            if (block.codeBlockPosition > 0)
                WriteAndResetBuffer(output, block);

            // Write header information
            WriteHeaderData(input, output, originalOutputPosition);
        }

        private void WriteAndResetBuffer(Stream output, Block block)
        {
            // Write data to output
            output.WriteByte(block.codeBlock);
            output.Write(block.buffer, 0, block.bufferLength);

            // Reset codeBlock and buffer
            block.codeBlock = 0;
            block.codeBlockPosition = 0;
            Array.Clear(block.buffer, 0, block.bufferLength);
            block.bufferLength = 0;
        }

        private void WriteHeaderData(Stream input, Stream output, long originalOutputPosition)
        {
            var outputEndPosition = output.Position;

            // Write header
            output.Position = originalOutputPosition;
            output.Write(Encoding.ASCII.GetBytes("ECD"), 0, 3);
            output.WriteByte(1);
            output.Write(new byte[4], 0, 4);
            output.Write(((int)output.Length - 0x10).GetArrayBigEndian(), 0, 4);
            output.Write(((int)input.Length).GetArrayBigEndian(), 0, 4);

            output.Position = outputEndPosition;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
