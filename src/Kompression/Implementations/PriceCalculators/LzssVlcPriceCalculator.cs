using Kontract.Kompression;
using System.Linq;

namespace Kompression.PatternMatch.PriceCalculators
{
    public class LzssVlcPriceCalculator : IPriceCalculator
    {
        private readonly int[] _runLengthThresholds = { 0x10, 0x80, 0x4000, 0x200000, 0x10000000 };

        public int CalculateLiteralPrice(int value, int literalRunLength, bool firstLiteralRun)
        {
            // For a literal run of 1 we have to account for the description byte and the literal
            // Totaling in 2 bytes
            if (literalRunLength == 1)
                return 16;

            // All those run lengths add another byte to the vlc representing the run length
            // Totaling in an increase of 2 bytes
            if (_runLengthThresholds.Contains(literalRunLength))
                return 16;

            // A normal literal being added
            return 8;
        }

        public int CalculateMatchPrice(int displacement, int length, int matchRunLength)
        {
            var bitLength = 0;

            // All those run lengths add another byte to the vlc representing the match run length
            // Totaling in an increase of 1 byte for the match run length
            if (_runLengthThresholds.Contains(matchRunLength))
                bitLength += 8;

            var displacementBitCount = GetBitCount(displacement - 1);
            var lengthBitCount = GetBitCount(length - 1);

            var lengthExtra = length > 1 && length <= 0x10 ? 0 : (lengthBitCount + 6) / 7;
            var displacementExtra = (displacementBitCount + 3) / 7;

            return bitLength + (lengthExtra + displacementExtra + 1) * 8;
        }

        private static int GetBitCount(long value)
        {
            var bitCount = 1;
            while ((value >>= 1) != 0)
                bitCount++;

            return bitCount;
        }
    }
}
