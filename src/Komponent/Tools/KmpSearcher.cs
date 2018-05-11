using System;
using System.IO;

/**
 * Author: James D. McCaffrey on WordPress
 * https://jamesmccaffrey.wordpress.com/2012/08/18/the-knuth-morris-pratt-string-search-algorithm-in-c/
 */

namespace Komponent.Tools
{
    public class KmpSearcher
    {
        private byte[] _w;
        private int[] _t;

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
                    if (_t[i] > -1)
                        i = _t[i];
                    else
                        i = 0;
                }
            }
            return -1;
        }
    }
}
