using System;
using System.IO;

/* Induced sorting suffix array creation reference: https://sites.google.com/site/yuta256/sais */

namespace Kompression.LempelZiv.Matcher.Models
{
    internal class SuffixArray
    {
        private const int MinBucketSize = 256;

        public readonly int[] Suffixes;
        public readonly int[] IndexLeft;

        public static SuffixArray Create(Stream input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var inputArray = new byte[input.Length - input.Position];
            var bkPos = input.Position;
            input.Read(inputArray, 0, inputArray.Length);
            input.Position = bkPos;

            var suffixes = new int[input.Length];
            var indexLeft = new int[256];
            for (int i = 0; i < 256; i++)
                indexLeft[i] = -1;

            Build(new ByteArray(inputArray, 0), inputArray.Length, suffixes, 0, 256);
            // Build left indexes
            indexLeft[inputArray[suffixes[0]]] = 0;
            byte lastValue = inputArray[suffixes[0]];
            for (int i = 1; i < suffixes.Length; i++)
                if (inputArray[suffixes[i]] != lastValue)
                {
                    lastValue = inputArray[suffixes[i]];
                    indexLeft[inputArray[suffixes[i]]] = i;
                }

            return new SuffixArray(suffixes, indexLeft);
        }

        public static SuffixArray Create(byte[] inputArray, int inputSize)
        {
            if (inputArray == null)
                throw new ArgumentNullException(nameof(inputArray));

            var suffixes = new int[inputSize];
            var indexLeft = new int[256];
            for (int i = 0; i < 256; i++)
                indexLeft[i] = -1;

            Build(new ByteArray(inputArray, 0), inputSize, suffixes, 0, 256);
            // Build left indexes
            indexLeft[inputArray[suffixes[0]]] = 0;
            byte lastValue = inputArray[suffixes[0]];
            for (int i = 1; i < suffixes.Length; i++)
                if (inputArray[suffixes[i]] != lastValue)
                {
                    lastValue = inputArray[suffixes[i]];
                    indexLeft[inputArray[suffixes[i]]] = i;
                }

            return new SuffixArray(suffixes, indexLeft);
        }

        private static void Build(IArray input, int inputLength, int[] suffixes, int fs, int k)
        {
            IntArray C, buckets, RA;
            int i, j, b, m, p, q, name, newfs;
            int c0, c1;
            uint flags = 0;

            if (k <= MinBucketSize)
            {
                C = new IntArray(new int[k], 0);
                if (k <= fs)
                {
                    buckets = new IntArray(suffixes, inputLength + fs - k);
                    flags = 1;
                }
                else
                {
                    buckets = new IntArray(new int[k], 0);
                    flags = 3;
                }
            }
            else if (k <= fs)
            {
                C = new IntArray(suffixes, inputLength + fs - k);
                if (k <= fs - k)
                {
                    buckets = new IntArray(suffixes, inputLength + fs - k * 2);
                    flags = 0;
                }
                else if (k <= MinBucketSize * 4)
                {
                    buckets = new IntArray(new int[k], 0);
                    flags = 2;
                }
                else
                {
                    buckets = C;
                    flags = 8;
                }
            }
            else
            {
                C = buckets = new IntArray(new int[k], 0);
                flags = 4 | 8;
            }

            /* stage 1: reduce the problem by at least 1/2
               sort all the LMS-substrings */
            GetCounts(input, C, inputLength, k);
            GetBuckets(C, buckets, k, true); /* find ends of buckets */
            for (i = 0; i < inputLength; ++i)
                suffixes[i] = 0;

            b = -1;
            i = inputLength - 1;
            j = inputLength;
            m = 0;
            c0 = input[inputLength - 1];
            do
            {
                c1 = c0;
            } while (0 <= --i && (c0 = input[i]) >= c1);
            for (; 0 <= i;)
            {
                do
                {
                    c1 = c0;
                } while (0 <= --i && (c0 = input[i]) <= c1);
                if (0 <= i)
                {
                    if (0 <= b)
                    {
                        suffixes[b] = j;
                    }
                    b = --buckets[c1];
                    j = i;
                    ++m;
                    do
                    {
                        c1 = c0;
                    } while (0 <= --i && (c0 = input[i]) >= c1);
                }
            }
            if (1 < m)
            {
                LmsSort(input, suffixes, C, buckets, inputLength, k);
                name = LmsPostProcess(input, suffixes, inputLength, m);
            }
            else if (m == 1)
            {
                suffixes[b] = j + 1;
                name = 1;
            }
            else
            {
                name = 0;
            }

            /* stage 2: solve the reduced problem
               recurse if names are not yet unique */
            if (name < m)
            {
                if ((flags & 4) != 0) { C = null; buckets = null; }
                if ((flags & 2) != 0) { buckets = null; }
                newfs = (inputLength + fs) - (m * 2);
                if ((flags & (1 | 4 | 8)) == 0)
                {
                    if (k + name <= newfs) { newfs -= k; }
                    else { flags |= 8; }
                }
                for (i = m + (inputLength >> 1) - 1, j = m * 2 + newfs - 1; m <= i; --i)
                {
                    if (suffixes[i] != 0) { suffixes[j--] = suffixes[i] - 1; }
                }
                Build(new IntArray(suffixes, m + newfs), m, suffixes, newfs, name);

                i = inputLength - 1; j = m * 2 - 1; c0 = input[inputLength - 1];
                do { c1 = c0; } while ((0 <= --i) && ((c0 = input[i]) >= c1));
                for (; 0 <= i;)
                {
                    do { c1 = c0; } while ((0 <= --i) && ((c0 = input[i]) <= c1));
                    if (0 <= i)
                    {
                        suffixes[j--] = i + 1;
                        do { c1 = c0; } while ((0 <= --i) && ((c0 = input[i]) >= c1));
                    }
                }

                for (i = 0; i < m; ++i) { suffixes[i] = suffixes[m + suffixes[i]]; }
                if ((flags & 4) != 0) { C = buckets = new IntArray(new int[k], 0); }
                if ((flags & 2) != 0) { buckets = new IntArray(new int[k], 0); }
            }

            /* stage 3: induce the result for the original problem */
            if ((flags & 8) != 0) { GetCounts(input, C, inputLength, k); }
            /* put all left-most S characters into their buckets */
            if (1 < m)
            {
                GetBuckets(C, buckets, k, true); /* find ends of buckets */
                i = m - 1; j = inputLength; p = suffixes[m - 1]; c1 = input[p];
                do
                {
                    q = buckets[c0 = c1];
                    while (q < j) { suffixes[--j] = 0; }
                    do
                    {
                        suffixes[--j] = p;
                        if (--i < 0) { break; }
                        p = suffixes[i];
                    } while ((c1 = input[p]) == c0);
                } while (0 <= i);
                while (0 < j) { suffixes[--j] = 0; }
            }
            InduceSuffixArray(input, suffixes, C, buckets, inputLength, k);
        }

        private static void GetCounts(IArray input, IArray C, int n, int k)
        {
            for (var i = 0; i < k; i++)
                C[i] = 0;
            for (var i = 0; i < n; i++)
                C[input[i]]++;
        }

        private static void GetBuckets(IArray C, IArray B, int k, bool end)
        {
            var sum = 0;
            for (var i = 0; i < k; i++)
            {
                sum += C[i];
                B[i] = sum - (end ? 0 : C[i]);
            }
        }

        /* sort all type LMS suffixes */
        private static void LmsSort(IArray input, int[] suffixes, IArray C, IArray B, int n, int k)
        {
            int b, i, j;
            int c0, c1;
            /* compute SAl */
            if (C == B) { GetCounts(input, C, n, k); }
            GetBuckets(C, B, k, false); /* find starts of buckets */
            j = n - 1;
            b = B[c1 = input[j]];
            --j;
            suffixes[b++] = (input[j] < c1) ? ~j : j;
            for (i = 0; i < n; ++i)
            {
                if (0 < (j = suffixes[i]))
                {
                    if ((c0 = input[j]) != c1) { B[c1] = b; b = B[c1 = c0]; }
                    --j;
                    suffixes[b++] = (input[j] < c1) ? ~j : j;
                    suffixes[i] = 0;
                }
                else if (j < 0)
                {
                    suffixes[i] = ~j;
                }
            }
            /* compute SAs */
            if (C == B) { GetCounts(input, C, n, k); }
            GetBuckets(C, B, k, true); /* find ends of buckets */
            for (i = n - 1, b = B[c1 = 0]; 0 <= i; --i)
            {
                if (0 < (j = suffixes[i]))
                {
                    if ((c0 = input[j]) != c1) { B[c1] = b; b = B[c1 = c0]; }
                    --j;
                    suffixes[--b] = (input[j] > c1) ? ~(j + 1) : j;
                    suffixes[i] = 0;
                }
            }
        }

        private static int LmsPostProcess(IArray input, int[] suffixes, int n, int m)
        {
            int i, j, p, q, plen, qlen, name;
            int c0, c1;
            bool diff;

            /* compact all the sorted substrings into the first m items of suffixes
                2*m must be not larger than inputLength (proveable) */
            for (i = 0; (p = suffixes[i]) < 0; ++i) { suffixes[i] = ~p; }
            if (i < m)
            {
                for (j = i, ++i; ; ++i)
                {
                    if ((p = suffixes[i]) < 0)
                    {
                        suffixes[j++] = ~p; suffixes[i] = 0;
                        if (j == m) { break; }
                    }
                }
            }

            /* store the length of all substrings */
            i = n - 1; j = n - 1; c0 = input[n - 1];
            do { c1 = c0; } while ((0 <= --i) && ((c0 = input[i]) >= c1));
            for (; 0 <= i;)
            {
                do { c1 = c0; } while ((0 <= --i) && ((c0 = input[i]) <= c1));
                if (0 <= i)
                {
                    suffixes[m + ((i + 1) >> 1)] = j - i; j = i + 1;
                    do { c1 = c0; } while ((0 <= --i) && ((c0 = input[i]) >= c1));
                }
            }

            /* find the lexicographic names of all substrings */
            for (i = 0, name = 0, q = n, qlen = 0; i < m; ++i)
            {
                p = suffixes[i]; plen = suffixes[m + (p >> 1)]; diff = true;
                if ((plen == qlen) && ((q + plen) < n))
                {
                    for (j = 0; (j < plen) && (input[p + j] == input[q + j]); ++j) { }
                    if (j == plen) { diff = false; }
                }
                if (diff) { ++name; q = p; qlen = plen; }
                suffixes[m + (p >> 1)] = name;
            }

            return name;
        }

        /* compute suffixes and BWT */
        private static void InduceSuffixArray(IArray input, int[] suffixes, IArray C, IArray B, int n, int k)
        {
            int b, i, j;
            int c0, c1;
            /* compute SAl */
            if (C == B) { GetCounts(input, C, n, k); }
            GetBuckets(C, B, k, false); /* find starts of buckets */
            j = n - 1;
            b = B[c1 = input[j]];
            suffixes[b++] = ((0 < j) && (input[j - 1] < c1)) ? ~j : j;
            for (i = 0; i < n; ++i)
            {
                j = suffixes[i]; suffixes[i] = ~j;
                if (0 < j)
                {
                    if ((c0 = input[--j]) != c1) { B[c1] = b; b = B[c1 = c0]; }
                    suffixes[b++] = ((0 < j) && (input[j - 1] < c1)) ? ~j : j;
                }
            }
            /* compute SAs */
            if (C == B) { GetCounts(input, C, n, k); }
            GetBuckets(C, B, k, true); /* find ends of buckets */
            for (i = n - 1, b = B[c1 = 0]; 0 <= i; --i)
            {
                if (0 < (j = suffixes[i]))
                {
                    if ((c0 = input[--j]) != c1) { B[c1] = b; b = B[c1 = c0]; }
                    suffixes[--b] = ((j == 0) || (input[j - 1] > c1)) ? ~j : j;
                }
                else
                {
                    suffixes[i] = ~j;
                }
            }
        }

        private SuffixArray(int[] suffixes, int[] indexLeft)
        {
            Suffixes = suffixes;
            IndexLeft = indexLeft;
        }
    }

    internal interface IArray
    {
        int this[int i]
        {
            get;
            set;
        }
    }

    internal class ByteArray : IDisposable, IArray
    {
        private byte[] _array;
        private readonly int _position;
        public ByteArray(byte[] array, int pos)
        {
            _position = pos;
            _array = array;
        }

        public int this[int i]
        {
            get => _array[_position + i];
            set => _array[_position + i] = (byte)value;
        }

        private void Dispose(bool disposing)
        {
            _array = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

    internal class IntArray : IDisposable, IArray
    {
        private int[] _array;
        private readonly int _position;
        public IntArray(int[] array, int pos)
        {
            _position = pos;
            _array = array;
        }

        public int this[int i]
        {
            get => _array[_position + i];
            set => _array[_position + i] = value;
        }

        private void Dispose(bool disposing)
        {
            _array = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
