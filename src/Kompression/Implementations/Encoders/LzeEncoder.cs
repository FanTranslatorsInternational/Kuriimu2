using System;
using System.IO;
using System.Text;
using Kompression.Extensions;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    // TODO: Refactor block class
    public class LzeEncoder : IEncoder
    {
        class Block
        {
            public byte codeBlock;
            public int codeBlockPosition = 0;

            // each buffer can be at max 4 triplets of uncompressed data; a triplet is 3 bytes
            public byte[] buffer = new byte[4 * 3];
            public int bufferLength;
        }

        private readonly IMatchParser _matchParser;

        public LzeEncoder(IMatchParser matchParser)
        {
            _matchParser = matchParser;
        }

        public void Encode(Stream input, Stream output)
        {
            var originalOutputPosition = output.Position;
            output.Position += 6;

            var block = new Block();

            var matches = _matchParser.ParseMatches(input);
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

            WriteAndResetBuffer(output, block);

            // Write header information
            WriteHeaderData(input, output, originalOutputPosition);
        }

        private void CompressRawData(Stream input, Stream output, Block block, int length)
        {
            while (length > 0)
            {
                if (block.codeBlockPosition == 4)
                    WriteAndResetBuffer(output, block);

                if (length >= 3)
                {
                    length -= 3;
                    block.codeBlock |= (byte)(3 << (block.codeBlockPosition++ << 1));
                    block.buffer[block.bufferLength++] = (byte)input.ReadByte();
                    block.buffer[block.bufferLength++] = (byte)input.ReadByte();
                    block.buffer[block.bufferLength++] = (byte)input.ReadByte();
                }
                else
                {
                    length--;
                    block.codeBlock |= (byte)(2 << (block.codeBlockPosition++ << 1));
                    block.buffer[block.bufferLength++] = (byte)input.ReadByte();
                }
            }
        }

        private void CompressMatchData(Stream input, Stream output, Block block, Match match)
        {
            if (block.codeBlockPosition == 4)
                WriteAndResetBuffer(output, block);

            if (match.Displacement <= 4)
            {
                block.codeBlock |= (byte)(1 << (block.codeBlockPosition++ << 1));

                var byte1 = ((match.Length - 2) << 2) | (match.Displacement - 1);
                block.buffer[block.bufferLength++] = (byte)byte1;
            }
            else
            {
                block.codeBlockPosition++;

                var byte1 = match.Displacement - 5;
                var byte2 = ((match.Length - 3) << 4) | ((match.Displacement - 5) >> 8);
                block.buffer[block.bufferLength++] = (byte)byte1;
                block.buffer[block.bufferLength++] = (byte)byte2;
            }

            input.Position += match.Length;
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

            // Create header values
            var uncompressedLength = ((int)input.Length).GetArrayLittleEndian();

            // Write header
            output.Position = originalOutputPosition;
            output.Write(Encoding.ASCII.GetBytes("Le"), 0, 2);
            output.Write(uncompressedLength, 0, 4);

            output.Position = outputEndPosition;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
