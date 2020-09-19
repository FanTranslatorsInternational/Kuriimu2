using Kontract.Kompression;

namespace Kompression.Implementations.PriceCalculators
{
    public class NintendoRlePriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            if (literalRunLength % 0x80 == 1)
                return 16;

            return 8;
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength, int firstValue)
        {
            return 16;
        }
    }
}
