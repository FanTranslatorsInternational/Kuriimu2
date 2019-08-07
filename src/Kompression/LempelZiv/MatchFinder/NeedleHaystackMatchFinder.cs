using System;

namespace Kompression.LempelZiv.MatchFinder
{
    public class NeedleHaystackMatchFinder : ILongestMatchFinder
    {
        private int[] _badCharHeuristic;

        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }
        public int WindowSize { get; }
        public int MinDisplacement { get; }

        public NeedleHaystackMatchFinder(int minMatchSize, int maxMatchSize, int windowSize, int minDisplacement)
        {
            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
            WindowSize = windowSize;
            MinDisplacement = minDisplacement;
        }

        public LzMatch FindLongestMatch(byte[] input, int position)
        {
            var searchResult = Search(input, position, input.Length);
            if (searchResult.Equals(default))
                return null;

            return new LzMatch(position, position - searchResult.hitp, searchResult.hitl);
        }

        public void Dispose()
        {
            Array.Clear(_badCharHeuristic, 0, _badCharHeuristic.Length);
            _badCharHeuristic = null;
        }

        /// <summary>
        /// Search for a match by the given restrictions in a given set of data.
        /// </summary>
        /// <param name="data">Input data.</param>
        /// <param name="dataPosition">Position at which to find a match.</param>
        /// <param name="dataSize">Size of input data.</param>
        private (int hitp, int hitl) Search(byte[] data, int dataPosition, long dataSize)
        {
            var maxLength = Math.Min(MaxMatchSize, dataSize - dataPosition);
            if (maxLength < MinMatchSize)
                return default;

            var maxPosition = Math.Max(0, dataPosition - WindowSize);

            var hitPosition = 0;
            var hitLength = MinMatchSize;

            if (maxPosition < dataPosition)
            {
                var needleIndex = 0;
                while (needleIndex <= dataPosition - maxPosition - MinDisplacement)
                {
                    // The initial needle has a length of MinMatchSize, since anything below is invalid
                    needleIndex = (int)FirstIndexOfNeedleInHaystack(
                        new Span<byte>(data, maxPosition, dataPosition + hitLength - maxPosition),
                        new Span<byte>(data, dataPosition, hitLength));

                    // Increase hitLength while values are still equal
                    // We do that to increase the needleLength in future searches to maximize found matches
                    while (hitLength < maxLength && data[dataPosition + hitLength] == data[maxPosition + needleIndex + hitLength])
                        hitLength++;

                    maxPosition += needleIndex;
                    hitPosition = maxPosition;

                    // hitLength is guaranteed to never exceed maxLength
                    // If we reached maxLength, we can already return
                    if (hitLength == maxLength)
                        return (hitPosition, hitLength);

                    maxPosition++;
                    hitLength++;
                    if (maxPosition > dataPosition - MinDisplacement)
                        break;
                }
            }

            if (hitLength < 4)
                hitLength = 1;

            hitLength--;
            return (hitPosition, hitLength);
        }

        private long FirstIndexOfNeedleInHaystack(Span<byte> haystack, Span<byte> needle)
        {
            FillBadCharHeuristic(needle, needle.Length);

            var haystackPosition = 0;
            // Compare needle in haystack until end of needle can't be part of haystack anymore
            // We want to match the whole needle at best backwards
            while (haystackPosition <= haystack.Length - needle.Length)
            {
                // Match needle backwards in the haystack
                var lengthToMatch = needle.Length - 1;
                while (lengthToMatch >= 0 && needle[lengthToMatch] == haystack[haystackPosition + lengthToMatch])
                    lengthToMatch--;

                // If whole needle could already be matched
                // lengthToMatch is always -1 if the needle could be completely matched
                if (lengthToMatch < 0)
                    return haystackPosition;

                // Else go forward in the haystack and try finding a longer match
                // Either advance in the haystack by 1 or depending on the rest of the needle needing matching
                haystackPosition += Math.Max(1, lengthToMatch - _badCharHeuristic[haystack[haystackPosition + lengthToMatch]]);
            }

            return -1;
        }

        private void FillBadCharHeuristic(Span<byte> input, int size)
        {
            if (_badCharHeuristic == null)
                _badCharHeuristic = new int[256];

            // Reset bad char heuristic
            for (var i = 0; i < 256; i++)
                _badCharHeuristic[i] = -1;

            // Set highest position of a value in bad char heuristic
            for (var i = 0; i < size; i++)
                _badCharHeuristic[input[i]] = i;
        }
    }
}
