namespace Kryptography.Extensions
{
    static class ByteArrayExtensions
    {
        public static string Stringify(this byte[] input, int length = -1)
        {
            var result = string.Empty;
            for (var i = 0; i < (length < 0 ? input.Length : length); i++)
                result += input[i].ToString("X2");
            return result;
        }

        public static void Increment(this byte[] input, long count, bool littleEndian)
        {
            if (!littleEndian)
                IncrementBigEndian(input, count);
            else
                IncrementLittleEndian(input, count);
        }

        public static void Decrement(this byte[] input, long count, bool littleEndian)
        {
            if (!littleEndian)
                DecrementBigEndian(input, count);
            else
                DecrementLittleEndian(input, count);
        }

        private static void IncrementLittleEndian(byte[] input, long count)
        {
            for (var i = 0; i < input.Length; i++)
            {
                if (count == 0)
                    break;

                var check = input[i];
                input[i] += (byte)count;
                count >>= 8;

                var off = 0;
                while (i + off + 1 < input.Length && input[i + off] < check)
                {
                    check = input[i + off + 1];
                    input[i + off + 1]++;
                    off++;
                }
            }
        }

        private static void IncrementBigEndian(byte[] input, long count)
        {
            for (var i = input.Length - 1; i >= 0; i--)
            {
                if (count == 0)
                    break;

                var check = input[i];
                input[i] += (byte)count;
                count >>= 8;

                var off = 0;
                while (i - off - 1 >= 0 && input[i - off] < check)
                {
                    check = input[i - off - 1];
                    input[i - off - 1]++;
                    off++;
                }
            }
        }

        private static void DecrementLittleEndian(byte[] input, long count)
        {
            for (var i = 0; i < input.Length; i++)
            {
                if (count == 0)
                    break;

                var check = input[i];
                input[i] -= (byte)count;
                count >>= 8;

                var off = 0;
                while (i + off + 1 < input.Length && input[i + off] > check)
                {
                    check = input[i + off + 1];
                    input[i + off + 1]--;
                    off++;
                }
            }
        }

        private static void DecrementBigEndian(byte[] input, long count)
        {
            for (var i = input.Length - 1; i >= 0; i--)
            {
                if (count == 0)
                    break;

                var check = input[i];
                input[i] -= (byte)count;
                count >>= 8;

                var off = 0;
                while (i - off - 1 >= 0 && input[i - off] > check)
                {
                    check = input[i - off - 1];
                    input[i - off - 1]--;
                    off++;
                }
            }
        }
    }
}
