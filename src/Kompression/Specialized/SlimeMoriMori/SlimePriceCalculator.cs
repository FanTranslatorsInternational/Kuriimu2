using System;
using Kompression.LempelZiv;
using Kompression.LempelZiv.PriceCalculators;

namespace Kompression.Specialized.SlimeMoriMori
{
    class SlimePriceCalculator : IPriceCalculator
    {
        private readonly int _compressionMode;

        public SlimePriceCalculator(int compressionMode)
        {
            _compressionMode = compressionMode;
        }

        public int CalculateLiteralLength(byte value)
        {
            // 1 flag bit
            // 6 bit value (huffman approximation)
            return 6 + 1;
        }

        public int CalculateMatchLength(LzMatch match)
        {
            switch (_compressionMode)
            {
                case 2:
                    if (match.Length > 15 + 3)
                    {
                        // variable length encoded match length
                        // an LZ match always encodes at least 4 bits outside the vle value
                        var length = (match.Length - 3) >> 4;

                        var result = 4;
                        while (length > 0)
                        {
                            // 4 bits per vle part
                            result += 4;
                            // 3 bits are actual value part
                            length >>= 3;
                        }

                        // 1 flag bit
                        // 3 set flag bits to mark vle match length
                        // n bits vle match length
                        // 1 flag bit
                        // 3 displacement index bits
                        // approximate 3 bits for displacement
                        return 1 + 3 + result + 1 + 3 + 3;
                    }
                    else
                    {
                        // 1 flag bit
                        // 3 displacement index bits
                        // 4 match length bits
                        // approximate 3 bits for displacement
                        return 1 + 3 + 4 + 3;
                    }
                default:
                    throw new InvalidOperationException("Compression mode not supported for price calculation.");
            }
        }
    }
}
