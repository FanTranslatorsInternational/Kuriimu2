using System;
using System.Runtime.CompilerServices;

/*
 * Author: James D. McCaffrey on WordPress
 * https://jamesmccaffrey.wordpress.com/2012/08/18/the-knuth-morris-pratt-string-search-algorithm-in-c/
 */

[assembly: InternalsVisibleTo("KoreUnitTests")]
namespace Kore.Utilities.Text.TextSearcher
{
    /// <summary>
    /// Knuth-Morris-Pratt String pattern searcher.
    /// </summary>
    internal class KmpTextSearcher : BaseTextSearcher
    {
        private readonly byte[] _w;
        private readonly int[] _t;

        /// <summary>
        /// Creates a new instance of <see cref="KmpTextSearcher"/>.
        /// </summary>
        /// <param name="w">The pattern to search for.</param>
        public KmpTextSearcher(byte[] w)
        {
            _w = new byte[w.Length];
            Array.Copy(w, _w, w.Length);
            _t = BuildTable(w);
        }

        /// <inheritdoc />
        protected override int SearchInternal(int length, Func<int, byte> getByteFunc)
        {
            var m = 0;
            var i = 0;

            while (m + i < length)
            {
                if (IsCancelled())
                    break;

                var value = getByteFunc(m + i);
                if (_w[i] == value)
                {
                    if (i == _w.Length - 1)
                        return m;

                    i++;
                }
                else
                {
                    m = m + i - _t[i];
                    i = _t[i] > -1 ? _t[i] : 0;
                }
            }

            return -1;
        }

        private static int[] BuildTable(byte[] w)
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
                    ++cnd;
                    result[pos] = cnd;
                    ++pos;
                }
                else if (cnd > 0)
                    cnd = result[cnd];
                else
                {
                    result[pos] = 0;
                    ++pos;
                }
            }
            return result;
        }
    }
}
