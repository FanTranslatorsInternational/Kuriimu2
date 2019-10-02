using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Implementations.PriceCalculators
{
    public class BackwardLz77PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            return 9;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            return 17;
        }
    }
}
