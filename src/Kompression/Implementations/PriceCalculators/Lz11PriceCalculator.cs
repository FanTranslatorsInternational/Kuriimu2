using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Implementations.PriceCalculators
{
    public class Lz11PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            return 9;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            if (length <= 0x10)
                return 17;

            if (length <= 0x110)
                return 25;

            return 33;
        }
    }
}
