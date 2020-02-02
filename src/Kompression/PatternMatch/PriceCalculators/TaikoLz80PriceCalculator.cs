using Kontract.Kompression;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class TaikoLz80PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            var literalCount = state.CountLiterals(position) % 0x100BE + 1;
            if (literalCount == 0xC0)
                return 16;
            if (literalCount == 0x40)
                return 16;

            return 8;
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            if (length >= 2 && length <= 5 && displacement >= 1 && displacement <= 0x10)
                return 8;
            if (length >= 3 && length <= 0x12 && displacement >= 1 && displacement <= 0x400)
                return 16;

            return 24;
        }
    }
}
