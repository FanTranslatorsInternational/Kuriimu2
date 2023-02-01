using Kontract.Kompression.Interfaces;

namespace Kompression.Implementations.PriceCalculators
{
    class SosLz3PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            return literalRunLength * 8 + CalculateVlc(literalRunLength);
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength, int firstValue)
        {
            return 16 + CalculateVlc(length);
        }

        private int CalculateVlc(int value)
        {
            if (value < 15)
                return 4;

            value -= 15;
            return 12 + value / 255 * 8;
        }
    }
}
