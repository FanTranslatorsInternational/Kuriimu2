using Kompression.PatternMatch;

namespace Kompression.Implementations.PriceCalculators
{
    public class Yay0PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value)
        {
            return 9;
        }

        public int CalculateMatchPrice(Match match)
        {
            if (match.Length < 0x12)
                return 17;

            return 25;
        }
    }
}
