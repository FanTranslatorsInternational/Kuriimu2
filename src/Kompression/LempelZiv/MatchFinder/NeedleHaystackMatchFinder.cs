using System;

namespace Kompression.LempelZiv.MatchFinder
{
    public class NeedleHaystackMatchFinder : ILongestMatchFinder
    {
        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }
        public int WindowSize { get; }

        public NeedleHaystackMatchFinder(int minMatchSize, int maxMatchSize, int windowSize)
        {
            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
            WindowSize = windowSize;
        }

        public LzMatch FindLongestMatch(byte[] input, int position)
        {
            var searchResult = Search(input, position, input.Length, MaxMatchSize);
            if (searchResult.Equals(default))
                return null;

            return new LzMatch(position, position - searchResult.hitp, searchResult.hitl);
        }

        public void Dispose()
        {
            // nothing to dispose
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data">input data</param>
        /// <param name="pos">position at which to find match</param>
        /// <param name="sz">size of input</param>
        /// <param name="cap">max match length</param>
        public (int hitp, int hitl) Search(byte[] data, int pos, long sz, int cap)
        {
            var ml = Math.Min(cap, sz - pos);
            if (ml < MinMatchSize)
                return default;

            var mp = Math.Max(0, pos - WindowSize);
            var hitp = 0;
            var hitl = 3;

            if (mp < pos)
            {
                var hl = (int)FirstIndexOfNeedleInHaystack(new Span<byte>(data, mp, pos + hitl - mp), new Span<byte>(data, pos, hitl));
                while (hl < pos - mp)
                {
                    while (hitl < ml && data[pos + hitl] == data[mp + hl + hitl])
                        hitl++;

                    mp += hl;
                    hitp = mp;
                    if (hitl == ml)
                        return (hitp, hitl);

                    mp++;
                    hitl++;
                    if (mp >= pos)
                        break;

                    hl = (int)FirstIndexOfNeedleInHaystack(new Span<byte>(data, mp, pos + hitl - mp), new Span<byte>(data, pos, hitl));
                }
            }

            if (hitl < 4)
                hitl = 1;

            hitl--;
            return (hitp, hitl);
        }

        public long FirstIndexOfNeedleInHaystack(Span<byte> haystack, Span<byte> needle)
        {
            var badChar = CreateBadCharHeuristic(needle, needle.Length);

            var s = 0;
            while (s <= haystack.Length - needle.Length)
            {
                var j = needle.Length - 1;
                while (j >= 0 && needle[j] == haystack[s + j])
                    j--;

                if (j < 0)
                    return s;

                s += Math.Max(1, j - badChar[haystack[s + j]]);
            }

            return -1;
        }

        private int[] CreateBadCharHeuristic(Span<byte> input, int size)
        {
            var badChar = new int[256];

            for (var i = 0; i < 256; i++)
                badChar[i] = -1;

            for (var i = 0; i < size; i++)
                badChar[input[i]] = i;

            return badChar;
        }
    }
}
