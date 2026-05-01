using System.Text;

namespace Komponent.Text
{
    /// <summary>
    /// A text searcher implementing the Knuth-Morris-Pratt algorithm
    /// </summary>
    /// <remarks>https://jamesmccaffrey.wordpress.com/2012/08/18/the-knuth-morris-pratt-string-search-algorithm-in-c/</remarks>
    public class KmpSearcher
    {
        private readonly byte[] _input;
        private readonly int[] _lookup;

        private KmpSearcher(byte[] input)
        {
            _input = input;
            _lookup = BuildLookup(input);
        }

        public static KmpSearcher Create(string input, Encoding encoding)
        {
            return new KmpSearcher(encoding.GetBytes(input));
        }

        public static KmpSearcher Create(byte[] input)
        {
            var buffer = new byte[input.Length];
            Array.Copy(input, buffer, input.Length);

            return new KmpSearcher(buffer);
        }

        public int Find(string needle, Encoding encoding)
        {
            return Find(encoding.GetBytes(needle));
        }

        public int Find(byte[] needle)
        {
            var m = 0;
            var i = 0;

            while (m + i < needle.Length)
            {
                if (_input[i] == needle[m + i])
                {
                    if (i == _input.Length - 1)
                        return m;

                    i++;
                }
                else
                {
                    m = m + i - _lookup[i];
                    i = _lookup[i] > -1 ? _lookup[i] : 0;
                }
            }

            return -1;
        }

        private static int[] BuildLookup(byte[] w)
        {
            var result = new int[w.Length];
            var pos = 2;
            var cnd = 0;

            result[0] = -1;
            result[1] = 0;

            while (pos < w.Length)
            {
                if (w[pos - 1] == w[cnd])
                {
                    result[pos++] = ++cnd;
                }
                else if (cnd > 0)
                {
                    cnd = result[cnd];
                }
                else
                {
                    result[pos++] = 0;
                }
            }

            return result;
        }
    }
}
