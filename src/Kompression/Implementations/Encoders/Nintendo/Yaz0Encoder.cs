using System;
using System.IO;
using System.Text;
using Kompression.Extensions;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;
using Kontract.Models.IO;

namespace Kompression.Implementations.Encoders.Nintendo
{
    // TODO: Refactor block class
    public class Yaz0Encoder : IEncoder
    {
        private readonly ByteOrder _byteOrder;
        private IMatchParser _matchParser;

        class Block
        {
            public byte codeBlock;
            public int codeBlockPosition = 8;

            // each buffer can be at max 8 pairs of compressed matches; a compressed match can be at max 3 bytes
            public byte[] buffer = new byte[8 * 3];
            public int bufferLength;
        }

        public Yaz0Encoder(ByteOrder byteOrder, IMatchParser matchParser)
        {
            _byteOrder = byteOrder;
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
                // Write any data before the match, to the buffer
                while (input.Position < match.Position)
                {
                    if (block.codeBlockPosition == 0)
                        WriteAndResetBuffer(output, block);

                    block.codeBlock |= (byte)(1 << --block.codeBlockPosition);
                    block.buffer[block.bufferLength++] = (byte)input.ReadByte();
                }

                // Write match data to the buffer
                var firstByte = (byte)((match.Displacement - 1) >> 8);
                var secondByte = (byte)(match.Displacement - 1);

                if (match.Length < 0x12)
                    // Since minimum _length should be 3 for Yay0, we get a minimum matchLength of 1 in this case
                    firstByte |= (byte)((match.Length - 2) << 4);

                if (block.codeBlockPosition == 0)
                    WriteAndResetBuffer(output, block);

                block.codeBlockPosition--; // Since a match is flagged with a 0 bit, we don't need a bit shift and just decrease the position
                block.buffer[block.bufferLength++] = firstByte;
                block.buffer[block.bufferLength++] = secondByte;
                if (match.Length >= 0x12)
                    block.buffer[block.bufferLength++] = (byte)(match.Length - 0x12);

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
            block.codeBlockPosition = 8;
            Array.Clear(block.buffer, 0, block.bufferLength);
            block.bufferLength = 0;
        }

        private void WriteHeaderData(Stream input, Stream output, long originalOutputPosition)
        {
            var outputEndPosition = output.Position;

            // Create header values
            var uncompressedLength = _byteOrder == ByteOrder.LittleEndian
                ? ((int)input.Length).GetArrayLittleEndian()
                : ((int)input.Length).GetArrayBigEndian();

            // Write header
            output.Position = originalOutputPosition;
            output.Write(Encoding.ASCII.GetBytes("Yaz0"), 0, 4);
            output.Write(uncompressedLength, 0, 4);
            output.Write(new byte[8], 0, 8);
            output.Position = outputEndPosition;
        }

        public void Dispose()
        {
        }
    }
}
