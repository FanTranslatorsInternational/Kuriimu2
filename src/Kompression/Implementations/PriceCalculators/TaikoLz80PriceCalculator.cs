using Kontract.Kompression;

namespace Kompression.Implementations.PriceCalculators
{
    public class TaikoLz80PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            var literalCount = literalRunLength % 0x100BE;
            if (literalCount == 0xC0)
                return 16;
            if (literalCount == 0x40)
                return 16;

            return 8;
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength)
        {
            if (length >= 2 && length <= 5 && displacement >= 1 && displacement <= 0x10)
                return 8;
            if (length >= 3 && length <= 0x12 && displacement >= 1 && displacement <= 0x400)
                return 16;

            return 24;
        }
    }
}
