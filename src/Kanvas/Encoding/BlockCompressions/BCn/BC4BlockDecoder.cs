using System;
using System.Collections.Generic;

namespace Kanvas.Encoding.BlockCompressions.BCn
{
    class BC4BlockDecoder
    {
        private static readonly Lazy<BC4BlockDecoder> Lazy = new Lazy<BC4BlockDecoder>(() => new BC4BlockDecoder());
        public static BC4BlockDecoder Instance => Lazy.Value;

        public IEnumerable<int> Process(ulong data)
        {
            // If alpha0 > alpha1, we store alpha linearly
            // Otherwise first 6 Values are linear, index 6 is 0 and index 7 is 255

            var (alpha0, alpha1) = ((ushort)data, (ushort)(data >> 8));

            for (var i = 0; i < 16; i++)
            {
                var code = (int)(data >> (16 + 3 * i)) & 7;

                if (alpha0 > alpha1)
                {
                    yield return code == 0 ? alpha0 :
                        code == 1 ? alpha1 :
                        Interpolate(alpha0, alpha0, 8 - code, 7);
                }
                else
                {
                    yield return code == 0 ? alpha0 :
                        code == 1 ? alpha1 :
                        code < 6 ? Interpolate(alpha0, alpha0, 6 - code, 5) :
                        code % 2 * 255;
                }
            }
        }

        private int Interpolate(int a, int b, int num, float den) =>
            (int)((num * a + (den - num) * b) / den);
    }
}
