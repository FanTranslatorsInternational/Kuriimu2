using Kontract.Kompression;

namespace Kompression.Implementations.PriceCalculators
{
    class Lz4PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            // Calculate price of only one literal
            var price = 8;

            // Add pricing of 'token' if first literal
            if (literalRunLength == 1)
                return price + 4;

            // Add additional 8 bits for each literal run length that adds another length byte
            if (literalRunLength - 0xF == 0 || (literalRunLength - 0xF) % 0xFF == 0)
                return price + 8;

            return price;
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength, int firstValue)
        {
            // Always 20 bits initially, since a match occupies at least the 4 low bits in 'token'
            // and 16 bits for the displacement
            var price = 20;

            if (matchRunLength < 0xF)
                return price;

            return (matchRunLength + 0xF0) / 0xFF * 8;
        }
    }
}
