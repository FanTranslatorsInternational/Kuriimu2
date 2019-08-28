using Kompression.PatternMatch;

namespace Kompression.Implementations.PriceCalculators
{
    public class Lz40PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value)
        {
            return 9;
        }

        public int CalculateMatchPrice(Match match)
        {
            if (match.Length <= 0xF)
                return 17;

            if (match.Length <= 0x10F)
                return 25;

            return 33;
        }
    }
}
