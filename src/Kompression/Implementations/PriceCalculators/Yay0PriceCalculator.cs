using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Implementations.PriceCalculators
{
    public class Yay0PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            return 9;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            if (length < 0x12)
                return 17;

            return 25;
        }
    }
}
