using System;
using Kontract.Kompression;

namespace Kompression.Implementations.PriceCalculators
{
    public class LzEncPriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            // Check if this is the first literal run
            if (firstLiteralRun)
            {
                // Special first raw data read
                if (literalRunLength == 1)
                    return 16;
                if (literalRunLength == 0xef)
                    return 16;
                if (literalRunLength > 0xef && (literalRunLength - 3 - 0xF) % 0xFF == 1)
                    return 16;

                return 8;
            }

            if (literalRunLength == 4)
                return 16;
            if (literalRunLength == 0x13)
                return 16;
            if (literalRunLength > 0x13 && (literalRunLength - 3 - 0xF) % 0xFF == 1)
                return 16;

            return 8;
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength, int firstValue)
        {
            var bitCount = displacement <= 0x4000 ? 5 : 3;

            length -= 2;
            if (length > (1 << bitCount) - 1)
            {
                var vlePrice = CalculateVariableLength(length, bitCount);
                return 24 + vlePrice;
            }

            return 24;
        }

        private int CalculateVariableLength(int length, int bitCount)
        {
            var bitValue = (1 << bitCount) - 1;
            if (length <= bitValue)
                throw new ArgumentOutOfRangeException(nameof(length));

            length -= bitValue;
            var fullBytes = length / 0xFF;
            var remainder = (byte)(length - fullBytes * 0xFF);
            return fullBytes + (remainder > 0 ? 1 : 0);
        }
    }
}
