using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Func<byte[], int, int> _readValue;
        private Func<byte[], int, int, int, int, int> _calculateMatchSize;

        private int _previousPosition = -1;

        private int _windowPos;
        private int _windowLen;

        //private int[] _offsetTable;
        private int[] _reversedOffsetTable;

        //private int[] _startTable;
        //private int[] _endTable;

        private FindLimitations _limits;
        private FindOptions _options;

        /// <summary>
        /// Creates a new instance of <see cref="HistoryMatchState"/>.
        /// </summary>
        /// <param name="input">The input this match represents.</param>
        /// <param name="limits">The limits to search sequences in.</param>
        /// <param name="options">The options to search sequences with.</param>
        public HistoryMatchState(byte[] input, FindLimitations limits, FindOptions options)
        {
            _limits = limits;
            _options = options;

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

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            PrepareOffsetTable(input, minLength);
            stopwatch.Stop();

            switch (_options.UnitSize)
            {
                case UnitSize.Byte:
                    _calculateMatchSize = CalculateMatchSizeByte;
                    break;

                case UnitSize.Short:
                    _calculateMatchSize = CalculateMatchSizeShort;
                    break;
            }
        }

        /// <summary>
        /// Finds matches at a certain position with the given limitations.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position to search from.</param>
        /// <returns>All matches found at this position.</returns>
        public IEnumerable<Match> FindMatchesAtPosition(byte[] input, int position)
        {
            var maxLength = _limits.MaxLength <= 0 ? input.Length : _limits.MaxLength;

            var cappedLength = Math.Min(input.Length - position, maxLength);

            if (cappedLength < _limits.MinLength)
                yield break;

            var longestMatchSize = _limits.MinLength - 1;
            for (var matchOffset = _reversedOffsetTable[position];
                matchOffset != -1 && position - matchOffset <= _limits.MaxDisplacement;
                matchOffset = _reversedOffsetTable[matchOffset])
            {
                // Check if match and current position have min distance to each other
                if (position - matchOffset < _limits.MinDisplacement)
                    continue;

                // Check that min length of a match is satisfied
                if (!IsMinLengthSatisfied(input, position, matchOffset))
                    continue;

                // Check last value of longest match position
                if (longestMatchSize >= _limits.MinLength &&
                    input[position + longestMatchSize] != input[matchOffset + longestMatchSize])
                    continue;

                // Calculate the length of a match
                var nMaxSize = cappedLength;
                var matchSize = _calculateMatchSize(input, position, matchOffset, _limits.MinLength, nMaxSize);

                if (matchSize > longestMatchSize)
                {
                    // Return all matches up to the longest
                    yield return new Match(position, position - matchOffset, matchSize);

                    longestMatchSize = matchSize;
                    if (longestMatchSize == cappedLength)
                        yield break;
                }
            }
        }

        private void PrepareOffsetTable(byte[] input, int minValueLength)
        {
            _reversedOffsetTable = Enumerable.Repeat(-1, input.Length).ToArray();

            var valueTable = Enumerable.Repeat(-1, (int)Math.Pow(256, minValueLength)).ToArray();
            for (var i = 0; i < input.Length - minValueLength; i++)
            {
                var value = _readValue(input, i);

                if (valueTable[value] >= 0)
                    _reversedOffsetTable[i] = valueTable[value];

                valueTable[value] = i;
            }
        }

        private bool IsMinLengthSatisfied(byte[] input, int inputPosition, int searchPosition)
        {
            var minLength = Math.Min(3, _limits.MinLength);
            for (var i = minLength; i < _limits.MinLength; i++)
            {
                if (input[searchPosition + i] != input[inputPosition + i])
                    return false;
            }

            return true;
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

        /// <inheritdoc cref="Dispose()"/>
        public void Dispose()
        {
            //_offsetTable = null;
            _reversedOffsetTable = null;

            //_startTable = null;
            //_endTable = null;

            _options = null;
            _limits = null;
        }
    }
}
