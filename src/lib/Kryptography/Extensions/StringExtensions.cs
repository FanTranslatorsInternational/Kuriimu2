using System;

namespace Kryptography.Extensions
{
    static class StringExtensions
    {
        public static byte[] Hexlify(this string hex, int length = -1)
        {
            var charCount = hex.Length;

            var result = new byte[length < 0 ? charCount / 2 : (length + 1) & ~1];
            for (var i = 0; i < (length < 0 ? charCount : length * 2); i += 2)
                result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return result;
        }
    }
}
