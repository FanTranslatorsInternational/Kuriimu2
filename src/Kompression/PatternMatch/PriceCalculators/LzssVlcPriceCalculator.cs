using Kompression.Interfaces;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class LzssVlcPriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            var literals = state.CountLiterals(position) + 1;
            if (literals == 1)
                return 16;

            if (literals >= 16 && (literals - 16) % 0x7F == 0)
                return 16;

            return 8;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            var bitLength = 0;

            var matches = state.CountMatches(position) + 1;
            if (matches >= 16 && (matches - 16) % 0x7F == 0)
                bitLength += 8;

            var dispBitCount = GetBitCount(displacement);
            bitLength += dispBitCount / 7 * 8 + (dispBitCount % 7 <= 3 ? 4 : 12);

            if (length <= 0xF)
                bitLength += 4;
            else
            {
                var lengthBitCount = GetBitCount(length);
                bitLength += 4 + lengthBitCount / 7 * 8 + (lengthBitCount % 7 > 0 ? 8 : 0);
            }

            return bitLength;
        }

        private static int GetBitCount(long value)
        {
            var bitCount = 0;
            while (value > 0)
            {
                bitCount++;
                value >>= 1;
            }

            return bitCount;
        }
    }
}
