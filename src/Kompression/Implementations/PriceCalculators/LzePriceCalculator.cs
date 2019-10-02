using System;
using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Implementations.PriceCalculators
{
    class LzePriceCalculator : IPriceCalculator
    {
        /* Here we work without pricing the flags */

        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            return 8;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            if (displacement > 4 && length > 0x12)
                throw new InvalidOperationException("Invalid match for Lze.");

            if (displacement <= 4)
                return 8;

            return 16;
        }
    }
}
