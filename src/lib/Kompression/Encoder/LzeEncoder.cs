using Komponent.IO;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder
{
    public class LzeEncoder : ILempelZivEncoder
    {
        internal class Block
        {
            public byte CodeBlock;
            public int CodeBlockPosition;

            // each buffer can be at max 4 triplets of uncompressed data; a triplet is 3 bytes
            public readonly byte[] Buffer = new byte[4 * 3];
            public int BufferLength;
        }

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new LzePriceCalculator())
                .FindPatternMatches().WithinLimitations(3, 0x12, 5, 0x1004)
                .AndFindPatternMatches().WithinLimitations(2, 0x41, 1, 4);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var originalOutputPosition = output.Position;
            output.Position += 6;

            var block = new Block();

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

        private static void CompressRawData(Stream input, Stream output, Block block, int length)
        {
            while (length > 0)
            {
                if (block.CodeBlockPosition == 4)
                    WriteAndResetBuffer(output, block);

                if (length >= 3)
                {
                    length -= 3;
                    block.CodeBlock |= (byte)(3 << (block.CodeBlockPosition++ << 1));
                    block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
                    block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
                    block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
                }
                else
                {
                    length--;
                    block.CodeBlock |= (byte)(2 << (block.CodeBlockPosition++ << 1));
                    block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
                }
            }
        }

        private static void CompressMatchData(Stream input, Stream output, Block block, LempelZivMatch lempelZivMatch)
        {
            if (block.CodeBlockPosition == 4)
                WriteAndResetBuffer(output, block);

            if (lempelZivMatch.Displacement <= 4)
            {
                block.CodeBlock |= (byte)(1 << (block.CodeBlockPosition++ << 1));

                var byte1 = lempelZivMatch.Length - 2 << 2 | lempelZivMatch.Displacement - 1;
                block.Buffer[block.BufferLength++] = (byte)byte1;
            }
            else
            {
                block.CodeBlockPosition++;

                var byte1 = lempelZivMatch.Displacement - 5;
                var byte2 = lempelZivMatch.Length - 3 << 4 | lempelZivMatch.Displacement - 5 >> 8;
                block.Buffer[block.BufferLength++] = (byte)byte1;
                block.Buffer[block.BufferLength++] = (byte)byte2;
            }

            input.Position += lempelZivMatch.Length;
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

            // Create header values
            using var bw = new BinaryWriterX(output, true);
            output.Position = originalOutputPosition;

            // Write header
            output.Position = originalOutputPosition;
            bw.WriteString("Le", writeNullTerminator: false);
            bw.Write((int)input.Length);

            output.Position = outputEndPosition;
        }
    }
}
