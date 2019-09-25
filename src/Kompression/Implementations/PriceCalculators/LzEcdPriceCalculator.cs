using Kompression.PatternMatch;

namespace Kompression.Implementations.PriceCalculators
{
    public class LzEcdPriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value)
        {
            return 9;
        }

        public int CalculateMatchPrice(Match match)
        {
            return 17;
        }
    }
}
