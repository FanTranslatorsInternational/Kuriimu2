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
        public DataType UnitLength { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="NeedleHaystackMatchFinder"/>.
        /// </summary>
        /// <param name="minMatchSize">The minimum size a match must have.</param>
        /// <param name="maxMatchSize">The maximum size a match must have.</param>
        /// <param name="windowSize">The window in which to search for matches.</param>
        /// <param name="minDisplacement">The minimum displacement to find matches at.</param>
        /// <param name="type">The type of a matchable unit.</param>
        public NeedleHaystackMatchFinder(int minMatchSize, int maxMatchSize, int windowSize, int minDisplacement, DataType type = DataType.Byte)
        {
            UnitLength = type;

            if (type == DataType.Short)
                if (MinMatchSize % 2 != 0 || MaxMatchSize % 2 != 0 || WindowSize % 2 != 0 || MinDisplacement % 2 != 0)
                    throw new ArgumentException("All values need to be dividable by the unit type.");

            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
            WindowSize = windowSize;
            MinDisplacement = minDisplacement;
        }

        /// <inheritdoc cref="FindLongestMatch"/>
        public Match FindLongestMatch(byte[] input, int position)
        {
            var searchResult = SearchLongest(input, position);
            if (searchResult.Equals(default))
                return null;

            return new Match(position, position - searchResult.hitp, searchResult.hitl);
        }

        /// <inheritdoc cref="FindAllMatches"/>
        public IEnumerable<Match> FindAllMatches(byte[] input, int position, int limit = -1)
        {
            var searchResults = SearchAll(input, position).Select(x => new Match(position, position - x.hitp, x.hitl));
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
            var remainingLength = data.Length - dataPosition;
            if (UnitLength == DataType.Short)
                remainingLength = remainingLength >> 1 << 1;

            var maxLength = Math.Min(MaxMatchSize, remainingLength);
            if (maxLength < MinMatchSize)
                yield break;

            var minPosition = 0;
            if (UnitLength == DataType.Short)
                minPosition = dataPosition & 0x1;
            var maxPosition = Math.Max(minPosition, dataPosition - WindowSize + minPosition);

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
                    yield return (needleIndex + minPosition, hitLength);

                    while (hitLength < maxLength)
                    {
                        if (data[dataPosition + hitLength] != data[maxPosition + needleIndex + hitLength])
                            break;
                        if (UnitLength == DataType.Short)
                            if (data[dataPosition + hitLength + 1] != data[maxPosition + needleIndex + hitLength + 1])
                                break;

                        hitLength += (int)UnitLength;
                        yield return (needleIndex + minPosition, hitLength);
                    }
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
            while (haystackPosition + haystackOffset < needlePosition && haystackOffset <= haystackLength - needleLength - MinDisplacement)
            {
                // Match needle backwards in the haystack
                var lengthToMatch = needleLength - (int)UnitLength;
                while (lengthToMatch >= 0)
                {
                    if (input[needlePosition + lengthToMatch] != input[haystackPosition + haystackOffset + lengthToMatch])
                        break;
                    if (UnitLength == DataType.Short)
                        if (input[needlePosition + lengthToMatch + 1] != input[haystackPosition + haystackOffset + lengthToMatch + 1])
                            break;

                    lengthToMatch -= (int)UnitLength;
                }

                // If whole needle could already be matched
                // lengthToMatch is always -1 if the needle could be completely matched
                if (lengthToMatch < 0)
                {
                    yield return haystackOffset;
                    haystackOffset += (int)UnitLength;
                    continue;
                }

                // Else go forward in the haystack and try finding a longer match
                // Either advance in the haystack by unitLength or depending on the rest of the needle needing matching
                var badCharValue = lengthToMatch - _badCharHeuristic[input[haystackPosition + haystackOffset + lengthToMatch]];
                if (UnitLength == DataType.Short)
                    badCharValue = badCharValue >> 1 << 1;
                haystackOffset += Math.Max((int)UnitLength, badCharValue);
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
            var remainingLength = data.Length - dataPosition;
            if (UnitLength == DataType.Short)
                remainingLength = remainingLength >> 1 << 1;

            var maxLength = Math.Min(MaxMatchSize, remainingLength);
            if (maxLength < MinMatchSize)
                return default;

            var minPosition = 0;
            if (UnitLength == DataType.Short)
                minPosition = dataPosition & 0x1;
            var maxPosition = Math.Max(minPosition, dataPosition - WindowSize + minPosition);

            var hitPosition = 0;
            var hitLength = MinMatchSize;

            if (maxPosition < dataPosition)
            {
                var needleIndex = -1;
                while (needleIndex <= dataPosition - maxPosition - MinDisplacement)
                {
                    // The initial needle has a length of MinMatchSize, since anything below is invalid
                    var firstIndex = FirstIndexOfNeedleInHaystack(data, maxPosition,
                        dataPosition - maxPosition + hitLength,
                        dataPosition, hitLength);

                    if (firstIndex == -1 && needleIndex == -1)
                        return default;
                    if (firstIndex == -1 && needleIndex >= 0)
                        return (hitPosition, hitLength - (int)UnitLength);
                    needleIndex = (int)firstIndex;

                    // Increase hitLength while values are still equal
                    // We do that to increase the needleLength in future searches to maximize found matches
                    while (hitLength < maxLength)
                    {
                        if (data[dataPosition + hitLength] != data[maxPosition + needleIndex + hitLength])
                            break;
                        if (UnitLength == DataType.Short)
                            if (data[dataPosition + hitLength + 1] != data[maxPosition + needleIndex + hitLength + 1])
                                break;

                        hitLength += (int)UnitLength;
                    }

                    maxPosition += needleIndex;
                    hitPosition = maxPosition;

                    // hitLength is guaranteed to never exceed maxLength
                    // If we reached maxLength, we can already return
                    if (hitLength == maxLength)
                        return (hitPosition, hitLength);

                    maxPosition += (int)UnitLength;
                    hitLength += (int)UnitLength;
                    if (maxPosition > dataPosition - MinDisplacement)
                        break;
                }
            }

            if (hitLength <= MinMatchSize)
                return default;

            return (hitPosition, hitLength - (int)UnitLength);
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
            while (haystackPosition + haystackOffset < needlePosition && haystackOffset <= haystackLength - needleLength - MinDisplacement)
            {
                // Match needle backwards in the haystack
                var lengthToMatch = needleLength - (int)UnitLength;
                while (lengthToMatch >= 0)
                {
                    if (input[needlePosition + lengthToMatch] != input[haystackPosition + haystackOffset + lengthToMatch])
                        break;
                    if (UnitLength == DataType.Short)
                        if (input[needlePosition + lengthToMatch + 1] != input[haystackPosition + haystackOffset + lengthToMatch + 1])
                            break;

                    lengthToMatch -= (int)UnitLength;
                }

                // If whole needle could already be matched
                // lengthToMatch is always -1 if the needle could be completely matched
                if (lengthToMatch < 0)
                    return haystackOffset;

                // Else go forward in the haystack and try finding a longer match
                // Either advance in the haystack by unitLength or depending on the rest of the needle needing matching
                var badCharValue = lengthToMatch - _badCharHeuristic[input[haystackPosition + haystackOffset + lengthToMatch]];
                if (UnitLength == DataType.Short)
                    badCharValue = badCharValue >> 1 << 1;
                haystackOffset += Math.Max((int)UnitLength, badCharValue);
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
