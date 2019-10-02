using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Implementations.PriceCalculators
{
    public class LzssVlcPriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            return 8;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            var bitLength = 0;

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
