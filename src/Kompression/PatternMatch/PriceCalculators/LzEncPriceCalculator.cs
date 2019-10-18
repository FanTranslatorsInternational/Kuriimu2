using System;
using Kompression.Interfaces;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class LzEncPriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            var literals = state.CountLiterals(position) + 1;

            if (!state.HasMatches(position))
            {
                // Special first raw data read
                if (literals == 1)
                    return 16;
                if (literals == 0xef)
                    return 16;
                if (literals > 0xef && (literals - 3 - 0xF) % 0xFF == 1)
                    return 16;

                return 8;
            }

            if (literals == 4)
                return 16;
            if (literals == 0x13)
                return 16;
            if (literals > 0x13 && (literals - 3 - 0xF) % 0xFF == 1)
                return 16;

            return 8;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
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
