using Kontract.Kompression;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class NintendoRlePriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            var literalCount = state.CountLiterals(position) % 0x80 + 1;
            if (literalCount == 1)
                return 16;

            return 8;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            return 16;
        }
    }
}
