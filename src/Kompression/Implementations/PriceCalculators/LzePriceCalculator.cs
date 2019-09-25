using System;
using Kompression.PatternMatch;

namespace Kompression.Implementations.PriceCalculators
{
    class LzePriceCalculator : IPriceCalculator
    {
        /* Here we work without pricing the flags */

        public int CalculateLiteralPrice(int value)
        {
            return 8;
        }

        public int CalculateMatchPrice(Match match)
        {
            if (match.Displacement > 4 && match.Length > 0x12)
                throw new InvalidOperationException("Invalid match for Lze.");

            if (match.Displacement <= 4)
                return 8;

            return 16;
        }
    }
}
