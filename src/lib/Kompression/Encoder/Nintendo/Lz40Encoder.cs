using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder.Nintendo
{
    public class Lz40Encoder : ILempelZivEncoder
    {
        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new Lz40PriceCalculator())
                .FindPatternMatches().WithinLimitations(0x3, 0x1010F, 1, 0xFFF);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x40, (byte)(input.Length & 0xFF), (byte)(input.Length >> 8 & 0xFF), (byte)(input.Length >> 16 & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            WriteCompressedData(input, output, [.. matches]);
        }

        internal static void WriteCompressedData(Stream input, Stream output, LempelZivMatch[] matches)
        {
            int bufferedBlocks = 0, blockBufferLength = 1, lzIndex = 0;
            byte[] blockBuffer = new byte[8 * 4 + 1];

            while (input.Position < input.Length)
            {
                if (bufferedBlocks >= 8)
                {
                    WriteBlockBuffer(output, blockBuffer, blockBufferLength);

                    bufferedBlocks = 0;
                    blockBufferLength = 1;
                }

                if (lzIndex < matches.Length && input.Position == matches[lzIndex].Position)
                {
                    blockBufferLength = WriteCompressedBlockToBuffer(matches[lzIndex], blockBuffer, blockBufferLength, bufferedBlocks);
                    input.Position += matches[lzIndex++].Length;
                }
                else
                {
                    blockBuffer[blockBufferLength++] = (byte)input.ReadByte();
                }

                bufferedBlocks++;
            }

            WriteBlockBuffer(output, blockBuffer, blockBufferLength);
        }

        private static int WriteCompressedBlockToBuffer(LempelZivMatch lzLempelZivMatch, byte[] blockBuffer, int blockBufferLength, int bufferedBlocks)
        {
            // mark the next block as compressed
            blockBuffer[0] |= (byte)(1 << 7 - bufferedBlocks);

            // the last 1.5 bytes are always the displacement
            blockBuffer[blockBufferLength] = (byte)((lzLempelZivMatch.Displacement & 0x0F) << 4);
            blockBuffer[blockBufferLength + 1] = (byte)(lzLempelZivMatch.Displacement >> 4 & 0xFF);

            if (lzLempelZivMatch.Length > 0x10F)
            {
                // case 1: (A)1 (CD) (EF GH) + (0x0)(0x110) = (DISP = A-C-D)(LEN = E-F-G-H)
                blockBuffer[blockBufferLength] |= 0x01;
                blockBufferLength += 2;
                blockBuffer[blockBufferLength++] = (byte)(lzLempelZivMatch.Length - 0x110 & 0xFF);
                blockBuffer[blockBufferLength] = (byte)(lzLempelZivMatch.Length - 0x110 >> 8 & 0xFF);
            }
            else if (lzLempelZivMatch.Length > 0xF)
            {
                // case 0; (A)0 (CD) (EF) + (0x0)(0x10) = (DISP = A-C-D)(LEN = E-F)
                blockBuffer[blockBufferLength] |= 0x00;
                blockBufferLength += 2;
                blockBuffer[blockBufferLength] = (byte)(lzLempelZivMatch.Length - 0x10 & 0xFF);
            }
            else
            {
                // case > 1: (A)(B) (CD) + (0x0)(0x0) = (DISP = A-C-D)(LEN = B)
                blockBuffer[blockBufferLength++] |= (byte)(lzLempelZivMatch.Length & 0x0F);
            }

            blockBufferLength++;
            return blockBufferLength;
        }

        private static void WriteBlockBuffer(Stream output, byte[] blockBuffer, int blockBufferLength)
        {
            output.Write(blockBuffer, 0, blockBufferLength);
            Array.Clear(blockBuffer, 0, blockBufferLength);
        }
    }
}
