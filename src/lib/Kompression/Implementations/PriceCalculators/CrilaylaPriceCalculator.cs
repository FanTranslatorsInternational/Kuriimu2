using System;
using Kontract.Kompression.Interfaces;

namespace Kompression.Implementations.PriceCalculators
{
    public class CrilaylaPriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            return 1 + 8;
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength, int firstValue)
        {
            var price = 1 + 13;
            return price + CalculateLengthBits(length);
        }

        internal static int CalculateLengthBits(int length)
        {
            length -= 3;
            if (length < 3)
                return 2;

            length -= 3;
            if (length < 7)
                return 5;

            length -= 7;
            if (length < 31)
                return 10;

            length -= 31;
            var result = 10;
            do
            {
                result += 8;
                if (length == 0xFF)
                    result += 8;

                length -= Math.Min(length, 0xFF);
            } while (length > 0);

            return result;
        }
    }
}
