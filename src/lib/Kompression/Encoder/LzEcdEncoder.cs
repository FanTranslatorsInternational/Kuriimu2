using Komponent.Contract.Enums;
using Komponent.IO;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder
{
    public class LzEcdEncoder : ILempelZivEncoder
    {
        internal class Block
        {
            public byte CodeBlock;
            public int CodeBlockPosition;

            // each buffer can be at max 8 pairs of compressed matches; a compressed match is 2 bytes
            public readonly byte[] Buffer = new byte[8 * 2];
            public int BufferLength;
        }

        private const int WindowBufferLength_ = 0x400;
        private const int PreBufferSize_ = 0x3BE;

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new LzEcdPriceCalculator())
                .FindPatternMatches().WithinLimitations(3, 0x42, 1, 0x400)
                .AdjustInput(input => input.Prepend(PreBufferSize_));
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var originalOutputPosition = output.Position;
            output.Position += 0x10;

            var block = new Block();

            foreach (var match in matches)
            {
                // Write any data before the match, to the uncompressed table
                while (input.Position < match.Position)
                {
                    if (block.CodeBlockPosition == 8)
                        WriteAndResetBuffer(output, block);

                    block.CodeBlock |= (byte)(1 << block.CodeBlockPosition++);
                    block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
                }

                // Write match data to the buffer
                var bufferPosition = (PreBufferSize_ + match.Position - match.Displacement) % WindowBufferLength_;
                var firstByte = (byte)bufferPosition;
                var secondByte = (byte)(bufferPosition >> 2 & 0xC0 | (byte)(match.Length - 3));

                if (block.CodeBlockPosition == 8)
                    WriteAndResetBuffer(output, block);

                block.CodeBlockPosition++; // Since a match is flagged with a 0 bit, we don't need a bit shift and just increase the position
                block.Buffer[block.BufferLength++] = firstByte;
                block.Buffer[block.BufferLength++] = secondByte;

                input.Position += match.Length;
            }

            // Write any data after last match, to the buffer
            while (input.Position < input.Length)
            {
                if (block.CodeBlockPosition == 8)
                    WriteAndResetBuffer(output, block);

                block.CodeBlock |= (byte)(1 << block.CodeBlockPosition++);
                block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
            }

            // Flush remaining buffer to stream
            if (block.CodeBlockPosition > 0)
                WriteAndResetBuffer(output, block);

            // Write header information
            WriteHeaderData(input, output, originalOutputPosition);
        }

        private static void WriteAndResetBuffer(Stream output, Block block)
        {
            // Write data to output
            output.WriteByte(block.CodeBlock);
            output.Write(block.Buffer, 0, block.BufferLength);

            // Reset codeBlock and buffer
            block.CodeBlock = 0;
            block.CodeBlockPosition = 0;
            Array.Clear(block.Buffer, 0, block.BufferLength);
            block.BufferLength = 0;
        }

        private static void WriteHeaderData(Stream input, Stream output, long originalOutputPosition)
        {
            var outputEndPosition = output.Position;

            // Write header
            using var bw = new BinaryWriterX(output, true, ByteOrder.BigEndian);
            output.Position = originalOutputPosition;

            bw.WriteString("ECD\x1", writeNullTerminator: false);
            bw.Write(0);
            bw.Write((int)(output.Length - 0x10));
            bw.Write((int)input.Length);

            output.Position = outputEndPosition;
        }
    }
}
