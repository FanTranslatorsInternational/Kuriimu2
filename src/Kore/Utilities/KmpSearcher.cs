using System;
using System.IO;

namespace Kore.Utilities
{
    /// <summary>
    /// Knuth-Morris-Pratt String pattern searcher.
    /// </summary>
    class KmpSearcher
    {
        private readonly byte[] _w;
        private readonly int[] _t;

        /// <summary>
        /// Creates a new instance of <see cref="KmpSearcher"/>.
        /// </summary>
        /// <param name="w">The pattern to search for.</param>
        public KmpSearcher(byte[] w)
        {
            _w = new byte[w.Length];
            Array.Copy(w, _w, w.Length);
            _t = BuildTable(w);
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

        /// <summary>
        /// Search a pattern in a given byte array.
        /// </summary>
        /// <param name="s">The byte array to search in.</param>
        /// <returns>First occurence of pattern.</returns>
        public int Search(byte[] s)
        {
            var m = 0;
            var i = 0;
            while (m + i < s.Length)
            {
                if (_w[i] == s[m + i])
                {
                    if (i == _w.Length - 1)
                        return m;
                    ++i;
                }
                else
                {
                    m = m + i - _t[i];
                    if (_t[i] > -1)
                        i = _t[i];
                    else
                        i = 0;
                }
            }
            return -1;
        }

        /// <summary>
        /// Search a pattern in a given binary reader.
        /// </summary>
        /// <param name="br">The binary reader to search in.</param>
        /// <returns>First occurence of pattern.</returns>
        public int Search(BinaryReader br)
        {
            var m = 0;
            var i = 0;
            while (m + i < br.BaseStream.Length)
            {
                br.BaseStream.Position = m + i;
                if (_w[i] == br.ReadByte())
                {
                    if (i == _w.Length - 1)
                        return m;
                    ++i;
                }
                else
                {
                    m = m + i - _t[i];
                    i = _t[i] > -1 ? _t[i] : 0;
                }
            }
            return -1;
        }
    }
}
