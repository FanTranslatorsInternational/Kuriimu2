using System;
using System.Collections.Generic;
using System.Linq;

namespace Kompression.LempelZiv.MatchFinder
{
    public class NeedleHaystackMatchFinder : ILongestMatchFinder, IAllMatchFinder
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
            var searchResult = SearchLongest(input, position, input.Length);
            if (searchResult.Equals(default))
                return null;

            return new LzMatch(position, position - searchResult.hitp, searchResult.hitl);
        }
        public IEnumerable<LzMatch> FindAllMatches(byte[] input, int position, int limit = -1)
        {
            var searchResults = SearchAll(input, position, input.Length).Select(x => new LzMatch(position, position - x.hitp, x.hitl));
            if (limit >= 0)
                searchResults = searchResults.Take(limit);
            return searchResults;
        }


        public void Dispose()
        {
            Array.Clear(_badCharHeuristic, 0, _badCharHeuristic.Length);
            _badCharHeuristic = null;
        }

        #region Finding all matches

        private IEnumerable<(int hitp, int hitl)> SearchAll(byte[] data, int dataPosition, long dataSize)
        {
            var maxLength = Math.Min(MaxMatchSize, dataSize - dataPosition);
            if (maxLength < MinMatchSize)
                yield break;

            var maxPosition = Math.Max(0, dataPosition - WindowSize);

            if (maxPosition < dataPosition)
            {
                // Get all indices of needle in max haystack
                var indexes = AllIndexesOfNeedleInHaystack(data, maxPosition, dataPosition - maxPosition + MinMatchSize,
                    dataPosition, MinMatchSize);

                foreach (var needleIndex in indexes)
                {
                    var hitLength = MinMatchSize;
                    while (hitLength < maxLength &&
                           data[dataPosition + hitLength] == data[maxPosition + needleIndex + hitLength])
                        hitLength++;
                    yield return (needleIndex, hitLength);
                }
            }
        }

        private IEnumerable<int> AllIndexesOfNeedleInHaystack(byte[] input, int haystackPosition, int hayStackLength, int needlePosition, int needleLength)
        {
            FillBadCharHeuristic(input, needlePosition, needleLength);

            var haystackPositionCounter = 0;
            // Compare needle in haystack until end of needle can't be part of haystack anymore
            // We want to match the whole needle at best backwards
            while (haystackPosition <= hayStackLength - needleLength - MinDisplacement)
            {
                // Match needle backwards in the haystack
                var lengthToMatch = needleLength - 1;
                while (lengthToMatch >= 0 && input[needlePosition + lengthToMatch] == input[haystackPosition + haystackPositionCounter + lengthToMatch])
                    lengthToMatch--;

                // If whole needle could already be matched
                // lengthToMatch is always -1 if the needle could be completely matched
                if (lengthToMatch < 0)
                    yield return haystackPositionCounter;

                // Else go forward in the haystack and try finding a longer match
                // Either advance in the haystack by 1 or depending on the rest of the needle needing matching
                haystackPosition += Math.Max(1, lengthToMatch - _badCharHeuristic[input[haystackPosition + haystackPositionCounter + lengthToMatch]]);
            }
        }

        #endregion

        #region Finding longest match

        /// <summary>
        /// SearchLongest for a match by the given restrictions in a given set of data.
        /// </summary>
        /// <param name="data">Input data.</param>
        /// <param name="dataPosition">Position at which to find a match.</param>
        /// <param name="dataSize">Size of input data.</param>
        private (int hitp, int hitl) SearchLongest(byte[] data, int dataPosition, long dataSize)
        {
            var maxLength = Math.Min(MaxMatchSize, dataSize - dataPosition);
            if (maxLength < MinMatchSize)
                return default;

            var maxPosition = Math.Max(0, dataPosition - WindowSize);

            var hitPosition = 0;
            var hitLength = MinMatchSize;

            if (maxPosition < dataPosition)
            {
                var needleIndex = -1;
                while (needleIndex <= dataPosition - maxPosition - MinDisplacement)
                {
                    // The initial needle has a length of MinMatchSize, since anything below is invalid
                    var firstIndex = FirstIndexOfNeedleInHaystack(data, maxPosition,
                        dataPosition + hitLength - maxPosition,
                        dataPosition, hitLength);

                    if (firstIndex == -1 && needleIndex == -1)
                        return default;
                    if (firstIndex == -1 && needleIndex >= 0)
                        return (hitPosition, hitLength - 1);
                    needleIndex = (int)firstIndex;

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

            if (hitLength <= MinMatchSize)
                return default;

            hitLength--;
            return (hitPosition, hitLength);
        }

        private long FirstIndexOfNeedleInHaystack(byte[] input, int haystackPosition, int haystackLength, int needlePosition, int needleLength)
        {
            FillBadCharHeuristic(input, needlePosition, needleLength);

            var haystackPositionCounter = 0;
            // Compare needle in haystack until end of needle can't be part of haystack anymore
            // We want to match the whole needle at best backwards
            while (haystackPosition <= haystackLength - needleLength - MinDisplacement)
            {
                // Match needle backwards in the haystack
                var lengthToMatch = needleLength - 1;
                while (lengthToMatch >= 0 && input[needlePosition + lengthToMatch] == input[haystackPosition + haystackPositionCounter + lengthToMatch])
                    lengthToMatch--;

                // If whole needle could already be matched
                // lengthToMatch is always -1 if the needle could be completely matched
                if (lengthToMatch < 0)
                    return haystackPositionCounter;

                // Else go forward in the haystack and try finding a longer match
                // Either advance in the haystack by 1 or depending on the rest of the needle needing matching
                haystackPosition += Math.Max(1, lengthToMatch - _badCharHeuristic[input[haystackPosition + haystackPositionCounter + lengthToMatch]]);
            }

            return -1;
        }

        #endregion

        private void FillBadCharHeuristic(byte[] input, int position, int size)
        {
            if (_badCharHeuristic == null)
                _badCharHeuristic = new int[256];

            // Reset bad char heuristic
            for (var i = 0; i < 256; i++)
                _badCharHeuristic[i] = -1;

            // Set highest position of a value in bad char heuristic
            for (var i = position; i < size; i++)
                _badCharHeuristic[input[i]] = i;
        }
    }
}
