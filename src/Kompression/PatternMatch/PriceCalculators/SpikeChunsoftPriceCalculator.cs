using System;
using Kompression.Interfaces;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class SpikeChunsoftPriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            var literals = state.CountLiterals(position) + 1;

            if (literals % 0x1FFF == 1)
                return 16;
            if (literals % 0x1FFF == 0x20)
                return 16;

            return 8;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            if (displacement == 0)
            {
                // Rle
                return 16 + (length - 4 > 0xF ? 8 : 0);
            }

            // Lz
            var price = 16;

            var cappedLength = Math.Max(length - 7, 0);
            price += (cappedLength / 0x1F) * 8;
            if (cappedLength % 0x1F > 0)
                price += 8;

            return price;
        }
    }
}
