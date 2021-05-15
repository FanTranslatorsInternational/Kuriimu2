using System;
using System.Collections.Generic;
using System.Linq;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.PatternMatch.MatchFinders.Support
{
    /// <summary>
    /// The state machine for <see cref="HistoryMatchFinder"/>.
    /// </summary>
    public class HistoryMatchState : IDisposable
    {
        private readonly Func<byte[], int, int> _readValue;
        private readonly Func<byte[], int, int, int, int, int> _calculateMatchSize;
        private readonly int _unitSize;

        private readonly int _valueLength;
        private int[] _offsetTable;

        private FindLimitations _limits;

        /// <summary>
        /// Creates a new instance of <see cref="HistoryMatchState"/>.
        /// </summary>
        /// <param name="input">The input this match represents.</param>
        /// <param name="limits">The limits to search sequences in.</param>
        /// <param name="unitSize">The size of a unit to be matched.</param>
        public HistoryMatchState(byte[] input, FindLimitations limits, UnitSize unitSize)
        {
            _limits = limits;

            // Determine unit size dependant delegates
            _unitSize = (int)unitSize;
            switch (unitSize)
            {
                case UnitSize.Byte:
                    _calculateMatchSize = CalculateMatchSizeByte;
                    break;

                case UnitSize.Short:
                    _calculateMatchSize = CalculateMatchSizeShort;
                    break;
            }

            // Determine value reading delegate, based on given limitations
            _valueLength = Math.Min(3, limits.MinLength) / _unitSize * _unitSize;
            switch (_valueLength)
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

            // Prepare chained list of offsets per value
            PrepareOffsetTable(input);
        }

        /// <summary>
        /// Finds matches at a certain position with the given limitations.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position to search from.</param>
        /// <returns>All matches found at this position.</returns>
        public AggregateMatch FindMatchesAtPosition(byte[] input, int position)
        {
            if (input.Length - position < _unitSize)
                return null;

            var maxLength = _limits.MaxLength <= 0 ? input.Length : _limits.MaxLength;
            var maxDisplacement = _limits.MaxDisplacement <= 0 ? input.Length : _limits.MaxDisplacement;

            var cappedLength = Math.Min(input.Length - position, maxLength);

            if (cappedLength < _limits.MinLength)
                return null;

            var result = new List<(int, int)>();
            var longestMatchSize = _limits.MinLength - 1;
            for (var matchOffset = _offsetTable[position];
                matchOffset != -1 && position - matchOffset <= maxDisplacement;
                matchOffset = _offsetTable[matchOffset])
            {
                // If longest match already goes to end of file
                if (position + longestMatchSize >= input.Length)
                    break;

                // Check if match and current position have min distance to each other
                if (position - matchOffset < _limits.MinDisplacement)
                    continue;

                // Check last value of longest match position
                if (longestMatchSize >= _limits.MinLength &&
                    input[position + longestMatchSize] != input[matchOffset + longestMatchSize])
                    continue;

                // Calculate the length of a match
                var nMaxSize = cappedLength;
                var matchSize = _calculateMatchSize(input, position, matchOffset, _valueLength, nMaxSize);

                if (matchSize > longestMatchSize)
                {
                    // Return all matches up to the longest
                    result.Add((position - matchOffset, matchSize));

                    longestMatchSize = matchSize;
                    if (longestMatchSize == cappedLength)
                        break;
                }
            }

            return new AggregateMatch(result);
        }

        private void PrepareOffsetTable(byte[] input)
        {
            _offsetTable = Enumerable.Repeat(-1, input.Length).ToArray();
            var valueTable = Enumerable.Repeat(-1, (int)Math.Pow(256, _valueLength)).ToArray();

            for (var i = 0; i < input.Length - _valueLength; i += _unitSize)
            {
                var value = _readValue(input, i);

                if (valueTable[value] != -1)
                    _offsetTable[i] = valueTable[value];

                valueTable[value] = i;
            }
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
                if (input.Length - (inputPosition + minSize) < 2)
                    break;

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
            _offsetTable = null;

            _limits = null;
        }
    }
}
