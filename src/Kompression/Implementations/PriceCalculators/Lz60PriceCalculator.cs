using Kontract.Kompression;

namespace Kompression.Implementations.PriceCalculators
{
    public class Lz60PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            return 9;
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength)
        {
            if (length <= 0xF)
                return 17;

            if (length <= 0x10F)
                return 25;

            return 33;
        }
    }
}
