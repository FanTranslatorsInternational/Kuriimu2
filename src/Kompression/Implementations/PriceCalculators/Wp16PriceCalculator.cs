using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Implementations.PriceCalculators
{
    class Wp16PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            return 8 + 8 + 1;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            return 1 + 8 + 8;
        }
    }
}
