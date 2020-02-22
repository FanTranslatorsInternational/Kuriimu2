using Kontract.Kompression;

namespace Kompression.Implementations.PriceCalculators
{
    public class Lz11PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            return 9;
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength, int firstValue)
        {
            if (length <= 0x10)
                return 17;

            if (length <= 0x110)
                return 25;

            return 33;
        }
    }
}
