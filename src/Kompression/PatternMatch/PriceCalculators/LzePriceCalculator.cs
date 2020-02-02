using System;
using Kontract.Kompression;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class LzePriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            var literalCount = state.CountLiterals(position) % 3 + 1;
            if (literalCount == 3)
                return 6;

            return 10;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            if (displacement > 4 && length > 0x12)
                throw new InvalidOperationException("Invalid match for Lze.");

            if (displacement <= 4)
                return 10;

            return 18;
        }
    }
}
