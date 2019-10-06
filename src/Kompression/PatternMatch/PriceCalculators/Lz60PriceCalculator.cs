using Kompression.Interfaces;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class Lz60PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            return 9;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            if (length <= 0xF)
                return 17;

            if (length <= 0x10F)
                return 25;

            return 33;
        }
    }
}
