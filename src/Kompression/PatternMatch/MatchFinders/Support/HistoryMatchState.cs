using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.Configuration;
using Kompression.Models;

namespace Kompression.PatternMatch.MatchFinders.Support
{
    /// <summary>
    /// The state machine for <see cref="HistoryMatchFinder"/>.
    /// </summary>
    public class HistoryMatchState : IDisposable
    {
        private readonly Func<byte[], int, int> _readValue;
        private readonly Func<byte[], int, int, int, int, int> _calculateMatchSize;

        private int[] _reversedOffsetTable;

        private FindLimitations _limits;

        /// <summary>
        /// Creates a new instance of <see cref="HistoryMatchState"/>.
        /// </summary>
        /// <param name="input">The input this match represents.</param>
        /// <param name="startPosition">The position at which to start pre process.</param>
        /// <param name="limits">The limits to search sequences in.</param>
        /// <param name="unitSize">The size of a unit to be matched.</param>
        public HistoryMatchState(byte[] input, int startPosition, FindLimitations limits, UnitSize unitSize)
        {
            _limits = limits;

            var minLength = Math.Min(3, limits.MinLength);
            switch (minLength)
            {
                case 3:
                    _readValue = ReadValue3;
                    break;

                case 2:
                    _readValue = ReadValue2;
                    break;

                default:
                    _readValue = ReadValue1;
                    break;
            }

            switch (unitSize)
            {
                case UnitSize.Byte:
                    _calculateMatchSize = CalculateMatchSizeByte;
                    break;

                case UnitSize.Short:
                    _calculateMatchSize = CalculateMatchSizeShort;
                    break;
            }

            PrepareOffsetTable(input, startPosition, minLength);
        }

        /// <summary>
        /// Finds matches at a certain position with the given limitations.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position to search from.</param>
        /// <returns>All matches found at this position.</returns>
        public IList<Match> FindMatchesAtPosition(byte[] input, int position)
        {
            var maxLength = _limits.MaxLength <= 0 ? input.Length : _limits.MaxLength;
            var minLength = Math.Min(3, _limits.MinLength);

            var cappedLength = Math.Min(input.Length - position, maxLength);

            if (cappedLength < _limits.MinLength)
                return Array.Empty<Match>();

            var result = new List<Match>();
            var longestMatchSize = _limits.MinLength - 1;
            for (var matchOffset = _reversedOffsetTable[position];
                matchOffset != -1 && position - matchOffset <= _limits.MaxDisplacement;
                matchOffset = _reversedOffsetTable[matchOffset])
            {
                // Check if match and current position have min distance to each other
                if (position - matchOffset < _limits.MinDisplacement)
                    continue;

                // Check last value of longest match position
                if (longestMatchSize >= _limits.MinLength &&
                    input[position + longestMatchSize] != input[matchOffset + longestMatchSize])
                    continue;

                // Calculate the length of a match
                var nMaxSize = cappedLength;
                var matchSize = _calculateMatchSize(input, position, matchOffset, minLength, nMaxSize);

                if (matchSize > longestMatchSize)
                {
                    // Return all matches up to the longest
                    result.Add(new Match(position, position - matchOffset, matchSize));

                    longestMatchSize = matchSize;
                    if (longestMatchSize == cappedLength)
                        break;
                }
            }

            return result;
        }

        private void PrepareOffsetTable(byte[] input, int startPosition, int minValueLength)
        {
            _reversedOffsetTable = Enumerable.Repeat(-1, input.Length).ToArray();

            var valueTable = Enumerable.Repeat(-1, (int)Math.Pow(256, minValueLength)).ToArray();
            for (var i = startPosition; i < input.Length - minValueLength; i++)
            {
                var value = _readValue(input, i);

                if (valueTable[value] >= 0)
                    _reversedOffsetTable[i] = valueTable[value];

                valueTable[value] = i;
            }

            valueTable = null;
        }

        private static int ReadValue1(byte[] input, int position)
        {
            return input[position];
        }

        private static int ReadValue2(byte[] input, int position)
        {
            return (input[position] << 8) | input[position + 1];
        }

        private static int ReadValue3(byte[] input, int position)
        {
            return (input[position] << 16) | (input[position + 1] << 8) | input[position + 2];
        }

        private int CalculateMatchSizeByte(byte[] input, int inputPosition, int searchPosition, int minSize, int maxSize)
        {
            while (minSize < maxSize)
            {
                if (input[searchPosition + minSize] != input[inputPosition + minSize])
                    break;

                minSize++;
            }

            return minSize;
        }

        private int CalculateMatchSizeShort(byte[] input, int inputPosition, int searchPosition, int minSize, int maxSize)
        {
            while (minSize < maxSize)
            {
                if (input[searchPosition + minSize] != input[inputPosition + minSize] ||
                    input[searchPosition + minSize + 1] != input[inputPosition + minSize + 1])
                    break;

                minSize += 2;
            }

            return minSize;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _reversedOffsetTable = null;

            _limits = null;
        }
    }
}
