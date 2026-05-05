using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder
{
    public class LzssVlcEncoder : ILempelZivEncoder
    {
        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new LzssVlcPriceCalculator())
                .FindPatternMatches().WithinLimitations(1, -1);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var decompressedSize = CreateVlc((int)input.Length);
            var unk1 = CreateVlc(0x19);
            var unk2 = CreateVlc(0);

            output.Write(decompressedSize, 0, decompressedSize.Length);
            output.Write(unk1, 0, unk1.Length);
            output.Write(unk2, 0, unk2.Length);

            WriteCompressedData(input, output, [.. matches]);
        }

        private static void WriteCompressedData(Stream input, Stream output, LempelZivMatch[] matches)
        {
            var matchIndex = 0;
            while (input.Position < input.Length)
            {
                var nextMatchPosition = matchIndex >= matches.Length ? input.Length : matches[matchIndex].Position;
                var rawSize = (int)(nextMatchPosition - input.Position);
                var compressedBlocks = 0;
                if (matchIndex < matches.Length)
                    compressedBlocks = GetContinuousMatches(matches, matchIndex);

                // Write size of literals and matches
                var lengthBlock = MakeLiteralCopies(rawSize, compressedBlocks);
                output.Write(lengthBlock, 0, lengthBlock.Length);

                // Write literals
                var literals = new byte[rawSize];
                _ = input.Read(literals, 0, literals.Length);
                output.Write(literals, 0, literals.Length);

                // Write matches
                foreach (var match in matches.Skip(matchIndex).Take(compressedBlocks))
                {
                    var matchBlock = MakeLengthOffset(match.Length, match.Displacement);
                    output.Write(matchBlock, 0, matchBlock.Length);

                    input.Position += match.Length;
                }

                matchIndex += compressedBlocks;
            }
        }

        #region Helper

        private static int GetContinuousMatches(LempelZivMatch[] matches, int startIndex)
        {
            var compressedBlocks = 0;
            var matchPosition = matches[startIndex].Position;
            while (compressedBlocks + startIndex < matches.Length &&
                   matches[compressedBlocks + startIndex].Position == matchPosition)
            {
                matchPosition += matches[compressedBlocks + startIndex].Length;
                compressedBlocks++;
            }

            return compressedBlocks;
        }

        private static byte[] MakeLiteralCopies(int literal, int copies)
        {
            int litNibble = 0, copNibble = 0;
            byte[] litExtra = [], copExtra = [];

            if (literal is > 0 and < 16) litNibble = literal;
            else litExtra = CreateVlc(literal);

            if (copies is > 0 and < 16) copNibble = copies;
            else copExtra = CreateVlc(copies);

            return [.. new[] { (byte)(litNibble | copNibble << 4) }.Concat(litExtra).Concat(copExtra)];
        }

        private static byte[] MakeLengthOffset(int length, int offset)
        {
            (length, offset) = (length - 1, offset - 1);

            int lenNibble = 0, offNibble;
            byte[] lenExtra = [], offExtra = [];

            if (length is > 0 and < 16) lenNibble = length;
            else lenExtra = CreateVlc(length);

            if (offset < 8) offNibble = offset << 1 | 1;
            else
            {
                var totalBits = GetBitCount(offset);
                var extraSize = (totalBits + 3) / 7;
                var bitsToShift = 7 * extraSize;

                var high = offset >> bitsToShift;
                var low = offset & (1 << bitsToShift) - 1;

                offNibble = high << 1;
                offExtra = [.. CreateVlc(low | 127 << bitsToShift).Skip(1)];
            }

            return [.. new[] { (byte)(offNibble | lenNibble << 4) }.Concat(offExtra).Concat(lenExtra)];
        }

        private static byte[] CreateVlc(int n)
        {
            var tmp = new Stack<byte>();
            tmp.Push((byte)(n << 1 | 1));
            n >>= 7;
            while (n != 0)
            {
                tmp.Push((byte)(n << 1));
                n >>= 7;
            }

            return [.. tmp];
        }

        private static int GetBitCount(long value)
        {
            var bitCount = 1;
            while ((value >>= 1) != 0)
                bitCount++;

            return bitCount;
        }

        #endregion
    }
}
