using Kompression.Interfaces;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class TalesOf03PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            return 9;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            if (displacement == 0 && length >= 0x13)
                // Longest RLE match
                return 25;

            // Otherwise default match cost
            return 17;
        }
    }
}
