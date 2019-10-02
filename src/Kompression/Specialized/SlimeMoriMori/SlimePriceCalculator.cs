using System;
using Kompression.Interfaces;
using Kompression.Models;

namespace Kompression.Specialized.SlimeMoriMori
{
    class SlimePriceCalculator : IPriceCalculator
    {
        private readonly int _compressionMode;
        private readonly int _huffmanMode;

        public SlimePriceCalculator(int compressionMode, int huffmanMode)
        {
            _compressionMode = compressionMode;
            _huffmanMode = huffmanMode;
        }

        public int CalculateLiteralPrice(IMatchState state, int position, int value)
        {
            // 1 flag bit
            // n bit value (huffman approximation)
            switch (_huffmanMode)
            {
                case 1:
                    return 4;
                case 2:
                    return 7;
                default:
                    return 9;
            }
        }

        public int CalculateMatchPrice(IMatchState state, int position, int displacement, int length)
        {
            switch (_compressionMode)
            {
                case 1:
                    // 1 flag bit
                    // 2 displacement index bits
                    // 3 bit displacement approximation
                    // 4 bits match length
                    return 10;
                case 2:
                    if (length > 18)
                    {
                        // variable length encoded match length
                        // an LZ match always encodes at least 4 bits outside the vle value
                        var vleLength = (length - 3) >> 4;

                        var result = 4;
                        while (vleLength > 0)
                        {
                            // 4 bits per vle part
                            result += 4;
                            // 3 bits are actual value part
                            vleLength >>= 3;
                        }

                        // 1 flag bit
                        // 3 set flag bits to mark vle match length
                        // n bits vle match length
                        // 1 flag bit
                        // 3 displacement index bits
                        // approximate displacement with 3 bits
                        return 1 + 3 + result + 1 + 3 + 3;
                    }
                    else
                    {
                        // 1 flag bit
                        // 3 displacement index bits
                        // 4 match length bits
                        // approximate displacement with 3 bits
                        return 1 + 3 + 4 + 3;
                    }
                case 3:
                    if (length > 18)
                    {
                        // variable length encoded match length
                        // an LZ match always encodes at least 3 bits outside the vle value
                        var vleLength = (length / 2 - 2) >> 3;

                        var result = 3;
                        while (vleLength > 0)
                        {
                            // 3 bits per vle part
                            result += 3;
                            // 2 bits are actual value part
                            vleLength >>= 2;
                        }

                        // 1 flag bit
                        // 2 set flag bits to mark vle match length
                        // n bits vle match length
                        // 1 flag bit
                        // 2 displacement index bits
                        // approximate displacement with 3 bits
                        return 1 + 2 + result + 1 + 2 + 3;
                    }
                    else
                    {
                        // 1 flag bit
                        // 2 displacement index bits
                        // 3 match length bits
                        // approximate displacement with 3 bits
                        return 1 + 2 + 3 + 3;
                    }
                case 5:
                    if (displacement == 0)
                        // 2 flag bits
                        // 6 bits match length
                        // 8 bit static value
                        return 2 + 6 + 8;
                    else
                        // 2 displacement index bits
                        // approximate displacement with 3 bits
                        // 6 bits match length
                        return 2 + 3 + 6;
                default:
                    throw new InvalidOperationException("Compression mode not supported for price calculation.");
            }
        }
    }
}
