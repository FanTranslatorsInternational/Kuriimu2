using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.DataClasses.Encoder.LempelZiv.MatchFinder;
using Kompression.Contract.Enums.Encoder.LempelZiv;

namespace Kompression.Encoder.LempelZiv.MatchFinder.HistoryMatch
{
    /// <summary>
    /// The state machine for <see cref="HistoryMatchFinder"/>.
    /// </summary>
    public class HistoryMatchState : IDisposable
    {
        private readonly LempelZivMatchFinderOptions _options;

        private readonly Func<byte[], int, int> _readValue;
        private readonly Func<byte[], int, int, int, int, int> _calculateMatchSize;

        private readonly int _valueLength;
        private readonly int[] _offsetTable;

        /// <summary>
        /// Creates a new instance of <see cref="HistoryMatchState"/>.
        /// </summary>
        /// <param name="input">The input this match represents.</param>
        /// <param name="options">the options to find matches with.</param>
        public HistoryMatchState(byte[] input, LempelZivMatchFinderOptions options)
        {
            _options = options;

            // Determine unit size dependant delegates
            _calculateMatchSize = options.UnitSize switch
            {
                UnitSize.Byte => CalculateMatchSizeByte,
                UnitSize.Short => CalculateMatchSizeShort,
                _ => throw new InvalidOperationException($"Unsupported unit size {options.UnitSize}.")
            };

            // Determine value reading delegate, based on given limitations
            _valueLength = Math.Min(3, options.Limitations.MinLength) / (int)options.UnitSize * (int)options.UnitSize;
            _readValue = _valueLength switch
            {
                3 => ReadValue3,
                2 => ReadValue2,
                _ => ReadValue1
            };

            // Prepare chained list of offsets per value
            _offsetTable = PrepareOffsetTable(input);
        }

        /// <summary>
        /// Finds matches at a certain position with the given limitations.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position to search from.</param>
        /// <returns>All matches found at this position.</returns>
        public LempelZivAggregateMatch? FindMatchesAtPosition(byte[] input, int position)
        {
            if (input.Length - position < (int)_options.UnitSize)
                return null;

            int maxLength = _options.Limitations.MaxLength <= 0 ? input.Length : _options.Limitations.MaxLength;
            int maxDisplacement = _options.Limitations.MaxDisplacement <= 0 ? input.Length : _options.Limitations.MaxDisplacement;

            int cappedLength = Math.Min(input.Length - position, maxLength);

            if (cappedLength < _options.Limitations.MinLength)
                return null;

            var result = new List<(int, int)>();
            int longestMatchSize = _options.Limitations.MinLength - 1;
            for (int matchOffset = _offsetTable[position];
                matchOffset != -1 && position - matchOffset <= maxDisplacement;
                matchOffset = _offsetTable[matchOffset])
            {
                // If longest match already goes to end of file
                if (position + longestMatchSize >= input.Length)
                    break;

                // Check if match and current position have min distance to each other
                if (position - matchOffset < _options.Limitations.MinDisplacement)
                    continue;

                // Check last value of longest match position
                if (longestMatchSize >= _options.Limitations.MinLength &&
                    input[position + longestMatchSize] != input[matchOffset + longestMatchSize])
                    continue;

                // Calculate the length of a match
                int nMaxSize = cappedLength;
                int matchSize = _calculateMatchSize(input, position, matchOffset, _valueLength, nMaxSize);

                if (matchSize <= longestMatchSize)
                    continue;

                // Return all matches up to the longest
                result.Add((position - matchOffset, matchSize));

                longestMatchSize = matchSize;
                if (longestMatchSize == cappedLength)
                    break;
            }

            return new LempelZivAggregateMatch(result);
        }

        private int[] PrepareOffsetTable(byte[] input)
        {
            int[] offsetTable = [.. Enumerable.Repeat(-1, input.Length)];
            int[] valueTable = [.. Enumerable.Repeat(-1, (int)Math.Pow(256, _valueLength))];

            for (var i = 0; i <= input.Length - _valueLength; i += (int)_options.UnitSize)
            {
                int value = _readValue(input, i);

                if (valueTable[value] != -1)
                    offsetTable[i] = valueTable[value];

                valueTable[value] = i;
            }

            return offsetTable;
        }

        private static int ReadValue1(byte[] input, int position)
        {
            return input[position];
        }

        private static int ReadValue2(byte[] input, int position)
        {
            return input[position] << 8 | input[position + 1];
        }

        private static int ReadValue3(byte[] input, int position)
        {
            return input[position] << 16 | input[position + 1] << 8 | input[position + 2];
        }

        private int CalculateMatchSizeByte(byte[] input, int inputPosition, int searchPosition, int minSize, int maxSize)
        {
            // OPTIMIZATION: Compare in batches of 4 bytes instead of single bytes

            var origPos = searchPosition;
            var maxPos = searchPosition + maxSize;

            inputPosition += minSize;
            searchPosition += minSize;

            while (searchPosition < maxPos)
            {
                if (input[searchPosition] != input[inputPosition])
                    return searchPosition - origPos;
                if (searchPosition + 1 >= maxPos || input[searchPosition + 1] != input[inputPosition + 1])
                    return searchPosition - origPos + 1;
                if (searchPosition + 2 >= maxPos || input[searchPosition + 2] != input[inputPosition + 2])
                    return searchPosition - origPos + 2;
                if (searchPosition + 3 >= maxPos || input[searchPosition + 3] != input[inputPosition + 3])
                    return searchPosition - origPos + 3;

                searchPosition += 4;
                inputPosition += 4;
            }

            return maxSize;
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
            GC.SuppressFinalize(this);
        }
    }
}
