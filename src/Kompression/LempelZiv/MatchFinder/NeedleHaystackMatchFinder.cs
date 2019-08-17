using System;
using System.Collections.Generic;
using System.Linq;

namespace Kompression.LempelZiv.MatchFinder
{
    /// <summary>
    /// Finds pattern matches by utilizing bad char heuristics and backward comparison of a given needle.
    /// </summary>
    public class NeedleHaystackMatchFinder : ILongestMatchFinder, IAllMatchFinder
    {
        private int[] _badCharHeuristic;

        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }
        public int WindowSize { get; }
        public int MinDisplacement { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="NeedleHaystackMatchFinder"/>.
        /// </summary>
        /// <param name="minMatchSize">The minimum size a match must have.</param>
        /// <param name="maxMatchSize">The maximum size a match must have.</param>
        /// <param name="windowSize">The window in which to search for matches.</param>
        /// <param name="minDisplacement">The minimum displacement to find matches at.</param>
        public NeedleHaystackMatchFinder(int minMatchSize, int maxMatchSize, int windowSize, int minDisplacement)
        {
            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
            WindowSize = windowSize;
            MinDisplacement = minDisplacement;
        }

        /// <inheritdoc cref="FindLongestMatch"/>
        public LzMatch FindLongestMatch(byte[] input, int position)
        {
            var searchResult = SearchLongest(input, position);
            if (searchResult.Equals(default))
                return null;

            return new LzMatch(position, position - searchResult.hitp, searchResult.hitl);
        }

        /// <inheritdoc cref="FindAllMatches"/>
        public IEnumerable<LzMatch> FindAllMatches(byte[] input, int position, int limit = -1)
        {
            var searchResults = SearchAll(input, position).Select(x => new LzMatch(position, position - x.hitp, x.hitl));
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

        /// <summary>
        /// Searches all matches at a given position.
        /// </summary>
        /// <param name="data">The input data.</param>
        /// <param name="dataPosition">The position to search matches at.</param>
        /// <returns>All matches at the given position.</returns>
        private IEnumerable<(int hitp, int hitl)> SearchAll(byte[] data, int dataPosition)
        {
            var maxLength = Math.Min(MaxMatchSize, data.Length - dataPosition);
            if (maxLength < MinMatchSize)
                yield break;

            var maxPosition = Math.Max(0, dataPosition - WindowSize);

            if (maxPosition < dataPosition)
            {
                // Get all indices of needle in max haystack
                var indexes = AllIndexesOfNeedleInHaystack(data, maxPosition, dataPosition - maxPosition - MinDisplacement + maxLength,
                    dataPosition, MinMatchSize);

                foreach (var needleIndex in indexes)
                {
                    if (needleIndex >= dataPosition)
                        throw new InvalidOperationException("NeedleIndex can't be beyond dataPosition.");

                    var hitLength = MinMatchSize;
                    yield return (needleIndex, hitLength);
                    while (hitLength < maxLength &&
                           data[dataPosition + hitLength] == data[maxPosition + needleIndex + hitLength])
                        yield return (needleIndex, ++hitLength);
                }
            }
        }

        /// <summary>
        /// Finds all indexes a given needle could be at.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="haystackPosition">The position of the haystack.</param>
        /// <param name="haystackLength">The length of the haystack.</param>
        /// <param name="needlePosition">The position of the needle.</param>
        /// <param name="needleLength">The length of the needle.</param>
        /// <returns>All needle indexes.</returns>
        private IEnumerable<int> AllIndexesOfNeedleInHaystack(byte[] input, int haystackPosition, int haystackLength, int needlePosition, int needleLength)
        {
            InitializeBadCharHeuristic(input, needlePosition, needleLength);

            var haystackOffset = 0;
            // Compare needle in haystack until end of needle can't be part of haystack anymore
            // We want to match the whole needle at best backwards
            while (haystackOffset + haystackPosition < needlePosition && haystackOffset <= haystackLength - needleLength - MinDisplacement)
            {
                // Match needle backwards in the haystack
                var lengthToMatch = needleLength - 1;
                while (lengthToMatch >= 0 &&
                       input[needlePosition + lengthToMatch] == input[haystackPosition + haystackOffset + lengthToMatch])
                    lengthToMatch--;

                // If whole needle could already be matched
                // lengthToMatch is always -1 if the needle could be completely matched
                if (lengthToMatch < 0)
                {
                    yield return haystackOffset;
                    haystackOffset++;
                    continue;
                }

                // Else go forward in the haystack and try finding a longer match
                // Either advance in the haystack by 1 or depending on the rest of the needle needing matching
                haystackOffset += Math.Max(1, lengthToMatch - _badCharHeuristic[input[haystackPosition + haystackOffset + lengthToMatch]]);
            }
        }

        #endregion

        #region Finding longest match

        /// <summary>
        /// Searches for the longest match at a given position.
        /// </summary>
        /// <param name="data">The input data.</param>
        /// <param name="dataPosition">The position to find the match at.</param>
        private (int hitp, int hitl) SearchLongest(byte[] data, int dataPosition)
        {
            var maxLength = Math.Min(MaxMatchSize, data.Length - dataPosition);
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

        /// <summary>
        /// Finds all indexes a given needle could be at.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="haystackPosition">The position of the haystack.</param>
        /// <param name="haystackLength">The length of the haystack.</param>
        /// <param name="needlePosition">The position of the needle.</param>
        /// <param name="needleLength">The length of the needle.</param>
        /// <returns>The needle index of the longest match.</returns>
        private long FirstIndexOfNeedleInHaystack(byte[] input, int haystackPosition, int haystackLength, int needlePosition, int needleLength)
        {
            InitializeBadCharHeuristic(input, needlePosition, needleLength);

            var haystackOffset = 0;
            // Compare needle in haystack until end of needle can't be part of haystack anymore
            // We want to match the whole needle at best backwards
            while (haystackOffset + haystackPosition < needlePosition && haystackOffset <= haystackLength - needleLength - MinDisplacement)
            {
                // Match needle backwards in the haystack
                var lengthToMatch = needleLength - 1;
                while (lengthToMatch >= 0 && input[needlePosition + lengthToMatch] == input[haystackPosition + haystackOffset + lengthToMatch])
                    lengthToMatch--;

                // If whole needle could already be matched
                // lengthToMatch is always -1 if the needle could be completely matched
                if (lengthToMatch < 0)
                    return haystackOffset;

                // Else go forward in the haystack and try finding a longer match
                // Either advance in the haystack by 1 or depending on the rest of the needle needing matching
                haystackOffset += Math.Max(1, lengthToMatch - _badCharHeuristic[input[haystackPosition + haystackOffset + lengthToMatch]]);
            }

            return -1;
        }

        #endregion

        /// <summary>
        /// Initializes the bad char heuristic table.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position the needle starts at.</param>
        /// <param name="size">The size the needle has.</param>
        private void InitializeBadCharHeuristic(byte[] input, int position, int size)
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
