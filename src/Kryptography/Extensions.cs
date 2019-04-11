using System;

namespace Kryptography
{
    internal static class Extensions
    {
        internal static void Increment(this byte[] input, long count, bool littleEndian)
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