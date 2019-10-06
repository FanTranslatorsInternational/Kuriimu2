using Kompression.Interfaces;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class Yaz0PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            return 9;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            if (length >= 0x12)
                return 25;

            return 17;
        }
    }
}
