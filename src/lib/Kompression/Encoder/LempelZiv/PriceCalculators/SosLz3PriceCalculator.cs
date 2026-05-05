using Kompression.Contract.Encoder.LempelZiv.PriceCalculator;

namespace Kompression.Encoder.LempelZiv.PriceCalculators
{
    internal class SosLz3PriceCalculator : ILempelZivPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            return literalRunLength * 8 + CalculateVlc(literalRunLength);
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength, int firstValue)
        {
            return 16 + CalculateVlc(length);
        }

        private static int CalculateVlc(int value)
        {
            if (value < 15)
                return 4;

            value -= 15;
            return 12 + value / 255 * 8;
        }
    }
}
