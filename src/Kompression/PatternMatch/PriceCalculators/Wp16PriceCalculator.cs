using Kontract.Kompression;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class Wp16PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            return 17;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            return 17;
        }
    }
}
