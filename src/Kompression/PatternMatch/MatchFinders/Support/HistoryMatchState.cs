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
        private Func<byte[], int, int> _readValue;
        private Func<byte[], int, int, int, int, int> _calculateMatchSize;

        private int _previousPosition = -1;

        private int _windowPos;
        private int _windowLen;

        private int[] _offsetTable;
        private int[] _reversedOffsetTable;

        private int[] _startTable;
        private int[] _endTable;

        private FindLimitations _limits;
        private FindOptions _options;

        /// <summary>
        /// Creates a new instance of <see cref="HistoryMatchState"/>.
        /// </summary>
        /// <param name="inputLength">The length of the future input to process.</param>
        /// <param name="limits">The limits to search sequences in.</param>
        /// <param name="options">The options to search sequences with.</param>
        public HistoryMatchState(int inputLength, FindLimitations limits, FindOptions options)
        {
            _limits = limits;
            _options = options;

            var minLength = Math.Min(3, limits.MinLength);
            _startTable = Enumerable.Repeat(-1, (int)Math.Pow(256, minLength)).ToArray();
            _endTable = Enumerable.Repeat(-1, (int)Math.Pow(256, minLength)).ToArray();

            var maxDisplacement = _limits.MaxDisplacement <= 0 ? inputLength : _limits.MaxDisplacement;
            _offsetTable = new int[maxDisplacement];
            _reversedOffsetTable = new int[maxDisplacement];

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

            if (_previousPosition == -1)
            {
                for (var i = 0; i < position; i += (int)_options.UnitSize)
                    SlideValue(input, i);
            }
            else
            {
                for (var i = 0; i < position - _previousPosition; i += (int)_options.UnitSize)
                    SlideValue(input, _previousPosition + i);
            }
            _previousPosition = position;

            var cappedLength = Math.Min(input.Length - position, maxLength);

            if (cappedLength < _limits.MinLength)
                yield break;

            var minLength = Math.Min(3, _limits.MinLength);
            var size = _limits.MinLength - 1;
            for (var matchOffset = _endTable[_readValue(input, position)]; matchOffset != -1; matchOffset = _reversedOffsetTable[matchOffset])
            {
                var search = position + matchOffset - _windowPos;
                if (matchOffset >= _windowPos)
                    search -= _windowLen;

                if (position - search < Math.Min(_limits.MinLength, _limits.MinDisplacement))
                    continue;

                // Check that min length of a match is satisfied
                if (!IsMinLengthSatisfied(input, position, search))
                    continue;

                // Calculate the length of a match
                var nMaxSize = cappedLength;
                var matchSize = _calculateMatchSize(input, position, search, _limits.MinLength, nMaxSize);

                if (matchSize > size)
                {
                    // Return all matches up to the longest
                    yield return new Match(position, position - search, matchSize);

                    size = matchSize;
                    if (size == cappedLength)
                        yield break;
                }
            }
        }

        private void SlideValue(byte[] input, int position)
        {
            var maxDisplacement = _limits.MaxDisplacement <= 0 ? input.Length : _limits.MaxDisplacement;

            var matchValue = _readValue(input, position);

            int firstOffset;
            if (_windowLen == maxDisplacement)
            {
                var startValue = _readValue(input, position - maxDisplacement);

                _startTable[startValue] = _offsetTable[_startTable[startValue]];
                if (_startTable[startValue] == -1)
                {
                    _endTable[startValue] = -1;
                }
                else
                {
                    _reversedOffsetTable[_startTable[startValue]] = -1;
                }

                firstOffset = _windowPos;
            }
            else
            {
                firstOffset = _windowLen;
            }

            var lastOffset = _endTable[matchValue];
            if (lastOffset == -1)
            {
                _startTable[matchValue] = firstOffset;
            }
            else
            {
                _offsetTable[lastOffset] = firstOffset;
            }

            _endTable[matchValue] = firstOffset;
            _offsetTable[firstOffset] = -1;
            _reversedOffsetTable[firstOffset] = lastOffset;

            if (_windowLen == maxDisplacement)
            {
                _windowPos += (int)_options.UnitSize;
                _windowPos %= maxDisplacement;
            }
            else
            {
                _windowLen += (int)_options.UnitSize;
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
            _offsetTable = null;
            _reversedOffsetTable = null;

            _startTable = null;
            _endTable = null;

            _options = null;
            _limits = null;
        }
    }
}
