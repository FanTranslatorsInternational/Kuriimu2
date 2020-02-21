using Kontract.Kompression;

namespace Kompression.Implementations.PriceCalculators
{
    class PsLzPriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            // TODO: Raw runs are not confirmed to use the same length encoding
            if ((literalRunLength - 1) % 0xFFFF == 0)
                return 16;

            if ((literalRunLength - 0x20) % 0xFFFF == 0)
                return 24;

            return 8;
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength)
        {
            var result = 0;

            if ((length - 1) % 0xFFFF == 0)
                result += 8;

            if ((length - 0x20) % 0xFFFF == 0)
                result += 24;

            if (displacement == 0)
                return result;

            result += displacement > 0xFF ? 16 : 8;
            return result;
        }
    }
}
