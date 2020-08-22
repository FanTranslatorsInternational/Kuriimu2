using System;
using System.Linq;

namespace Kanvas.Encoding.BlockCompressions.ETC1
{
    internal static class Constants
    {
        public static readonly int[] ZOrder = { 0, 4, 1, 5, 8, 12, 9, 13, 2, 6, 3, 7, 10, 14, 11, 15 };
        public static readonly int[] NormalOrder = { 0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15 };

        public static readonly int[][] Modifiers =
        {
            new[] {  2,   8,  -2,   -8 },
            new[] {  5,  17,  -5,  -17 },
            new[] {  9,  29,  -9,  -29 },
            new[] { 13,  42, -13,  -42 },
            new[] { 18,  60, -18,  -60 },
            new[] { 24,  80, -24,  -80 },
            new[] { 33, 106, -33, -106 },
            new[] { 47, 183, -47, -183 }
        };

        public static readonly int[] StaticColorLookup =
            new[] { 16, 32 }
            .SelectMany(limit => Modifiers
                .SelectMany(modifier => modifier
                    .SelectMany(selector => Enumerable.Range(0, 256)
                        .Select(color => Enumerable.Range(0, limit)
                            .Min(packedColor => CalculatePackedColor(limit, packedColor, selector, color))))))
            .ToArray();

        private static int CalculatePackedColor(int limit, int packedColor, int selector, int color)
        {
            var c = limit == 32 ? (packedColor << 3) | (packedColor >> 2) : packedColor * 17;
            return (Math.Abs(Clamp(c + selector) - color) << 8) | packedColor;
        }

        private static int Clamp(int n) => Math.Max(0, Math.Min(n, 255));
    }
}
