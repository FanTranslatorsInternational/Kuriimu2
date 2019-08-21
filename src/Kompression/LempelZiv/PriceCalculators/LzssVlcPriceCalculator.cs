namespace Kompression.LempelZiv.PriceCalculators
{
    class LzssVlcPriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralLength(byte value)
        {
            return 8;
        }

        public int CalculateMatchLength(IMatch match)
        {
            var bitLength = 0;

            var dispBitCount = GetBitCount(match.Displacement);
            bitLength += dispBitCount / 7 * 8 + (dispBitCount % 7 <= 3 ? 4 : 12);

            if (match.Length <= 0xF)
                bitLength += 4;
            else
            {
                var lengthBitCount = GetBitCount(match.Length);
                bitLength += 4 + lengthBitCount / 7 * 8 + (lengthBitCount % 7 > 0 ? 8 : 0);
            }

            return bitLength;
        }

        private static int GetBitCount(long value)
        {
            var bitCount = 0;
            while (value > 0)
            {
                bitCount++;
                value >>= 1;
            }

            return bitCount;
        }
    }
}
