using System;

namespace Kryptography
{
    internal static class Extensions
    {
        internal static byte[] Hexlify(this string hex, int length = -1)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[(length < 0) ? NumberChars / 2 : (length + 1) & ~1];
            for (int i = 0; i < ((length < 0) ? NumberChars : length * 2); i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        internal static void Increment(this byte[] input, int count, bool littleEndian)
        {
            if (!littleEndian)
                for (int i = input.Length - 1; i >= 0; i--)
                {
                    if (count == 0)
                        break;

                    var check = input[i];
                    input[i] += (byte)count;
                    count >>= 8;

                    int off = 0;
                    while (i - off - 1 >= 0 && input[i - off] < check)
                    {
                        check = input[i - off - 1];
                        input[i - off - 1]++;
                        off++;
                    }
                }
            else
                for (int i = 0; i < input.Length; i++)
                {
                    if (count == 0)
                        break;

                    var check = input[i];
                    input[i] += (byte)count;
                    count >>= 8;

                    int off = 0;
                    while (i + off + 1 < input.Length && input[i + off] < check)
                    {
                        check = input[i + off + 1];
                        input[i + off + 1]++;
                        off++;
                    }
                }
        }
    }
}