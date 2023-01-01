﻿using Kontract.Kompression.Interfaces;

namespace Kompression.Implementations.PriceCalculators
{
    public class Yaz0PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            return 9;
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength, int firstValue)
        {
            if (length >= 0x12)
                return 25;

            return 17;
        }
    }
}
