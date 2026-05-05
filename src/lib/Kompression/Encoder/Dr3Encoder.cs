using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder
{
    // TODO: Refactor block class
    public class Dr3Encoder : ILempelZivEncoder
    {
        internal class Block
        {
            public byte CodeBlock;
            public int CodeBlockPosition = 8;

            // each buffer can be at max 8 pairs of compressed matches; a compressed match is 2 bytes
            public readonly byte[] Buffer = new byte[8 * 2];
            public int BufferLength;
        }

        private const int WindowBufferLength_ = 0x400;
        private const int PreBufferSize_ = 0x3FA;

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new Dr3PriceCalculator())
                .FindPatternMatches().WithinLimitations(2, 0x41, 1, WindowBufferLength_)
                .AdjustInput(config => config.Prepend(PreBufferSize_));
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var block = new Block();

            foreach (var match in matches)
            {
                // Write any data before the match, to the uncompressed table
                while (input.Position < match.Position)
                {
                    if (block.CodeBlockPosition == 0)
                        WriteAndResetBuffer(output, block);

                    block.CodeBlock |= (byte)(1 << --block.CodeBlockPosition);
                    block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
                }

                // Write match data to the buffer
                var bufferPosition = WindowBufferLength_ - match.Displacement;
                var firstByte = (byte)bufferPosition;
                var secondByte = (byte)(bufferPosition >> 8 | (byte)(match.Length - 2 << 2));

                if (block.CodeBlockPosition == 0)
                    WriteAndResetBuffer(output, block);

                block.CodeBlockPosition--; // Since a match is flagged with a 0 bit, we don't need a bit shift and just decrease the position
                block.Buffer[block.BufferLength++] = firstByte;
                block.Buffer[block.BufferLength++] = secondByte;

                input.Position += match.Length;
            }

            // Write any data after last match, to the buffer
            while (input.Position < input.Length)
            {
                if (block.CodeBlockPosition == 0)
                    WriteAndResetBuffer(output, block);

                block.CodeBlock |= (byte)(1 << --block.CodeBlockPosition);
                block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
            }

            // Flush remaining buffer to stream
            if (block.CodeBlockPosition > 0)
                WriteAndResetBuffer(output, block);
        }

        private static void WriteAndResetBuffer(Stream output, Block block)
        {
            // Write data to output
            output.WriteByte(block.CodeBlock);
            output.Write(block.Buffer, 0, block.BufferLength);

            // Reset codeBlock and buffer
            block.CodeBlock = 0;
            block.CodeBlockPosition = 8;
            Array.Clear(block.Buffer, 0, block.BufferLength);
            block.BufferLength = 0;
        }
    }
}
