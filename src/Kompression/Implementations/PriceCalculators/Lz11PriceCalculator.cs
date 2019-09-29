using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Implementations.PriceCalculators
{
    public class Lz11PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value)
        {
            return 9;
        }

        public int CalculateMatchPrice(Match match)
        {
            if (match.Length <= 0x10)
                return 17;

            if (match.Length <= 0x110)
                return 25;

            return 33;
        }
    }
}
