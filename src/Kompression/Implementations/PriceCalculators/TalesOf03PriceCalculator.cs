using Kontract.Kompression;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class TalesOf03PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            return 9;
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength)
        {
            if (displacement == 0 && length >= 0x13)
                // Longest RLE match
                return 25;

            // Otherwise default match cost
            return 17;
        }
    }
}
