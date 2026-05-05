using Komponent.Contract.Enums;
using Komponent.IO;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder.Nintendo
{
    // TODO: Refactor block class
    public class Yaz0Encoder(ByteOrder byteOrder) : ILempelZivEncoder
    {
        internal class Block
        {
            public byte CodeBlock;
            public int CodeBlockPosition = 8;

            // each buffer can be at max 8 pairs of compressed matches; a compressed match can be at max 3 bytes
            public readonly byte[] Buffer = new byte[8 * 3];
            public int BufferLength;
        }

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new Yaz0PriceCalculator())
                .FindPatternMatches().WithinLimitations(3, 0x111, 1, 0x1000);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var originalOutputPosition = output.Position;
            output.Position += 0x10;

            var block = new Block();

            foreach (var match in matches)
            {
                // Write any data before the match, to the buffer
                while (input.Position < match.Position)
                {
                    if (block.CodeBlockPosition == 0)
                        WriteAndResetBuffer(output, block);

                    block.CodeBlock |= (byte)(1 << --block.CodeBlockPosition);
                    block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
                }

                // Write match data to the buffer
                var firstByte = (byte)(match.Displacement - 1 >> 8);
                var secondByte = (byte)(match.Displacement - 1);

                if (match.Length < 0x12)
                    // Since minimum _length should be 3 for Yay0, we get a minimum matchLength of 1 in this case
                    firstByte |= (byte)(match.Length - 2 << 4);

                if (block.CodeBlockPosition == 0)
                    WriteAndResetBuffer(output, block);

                block.CodeBlockPosition--; // Since a match is flagged with a 0 bit, we don't need a bit shift and just decrease the position
                block.Buffer[block.BufferLength++] = firstByte;
                block.Buffer[block.BufferLength++] = secondByte;
                if (match.Length >= 0x12)
                    block.Buffer[block.BufferLength++] = (byte)(match.Length - 0x12);

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
            block.CodeBlockPosition = 8;
            Array.Clear(block.Buffer, 0, block.BufferLength);
            block.BufferLength = 0;
        }

        private void WriteHeaderData(Stream input, Stream output, long originalOutputPosition)
        {
            var outputEndPosition = output.Position;

            // Write header
            using var bw = new BinaryWriterX(output, true, byteOrder);

            output.Position = originalOutputPosition;
            bw.WriteString("Yaz0", writeNullTerminator: false);
            bw.Write((int)input.Length);
            bw.Write(0L);
            output.Position = outputEndPosition;
        }
    }
}
