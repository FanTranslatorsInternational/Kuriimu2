using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.PatternMatch.LempelZiv.Support;

namespace Kompression.PatternMatch.LempelZiv
{
    /// <summary>
    /// Finding matches using <see cref="SuffixArray"/>.
    /// </summary>
    public class SuffixArrayMatchFinder : IMatchFinder
    {
        private readonly SuffixArray _array;

        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }
        public int MinDisplacement { get; }
        public int MaxDisplacement { get; }
        public DataType DataType { get; }
        public bool UseLookAhead { get; }

        public SuffixArrayMatchFinder(int minMatchSize, int maxMatchSize, int minDisplacement, int maxDisplacement,
            bool lookAhead = true, DataType dataType = DataType.Byte)
        {
            _array = new SuffixArray();

            if (minMatchSize % (int)dataType != 0 || maxMatchSize % (int)dataType != 0 ||
                minDisplacement % (int)dataType != 0 || maxDisplacement % (int)dataType != 0)
                throw new InvalidOperationException("All values must be dividable by data type.");

            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
            MinDisplacement = minDisplacement;
            MaxDisplacement = maxDisplacement;
            DataType = dataType;
            UseLookAhead = lookAhead;
        }

        public IEnumerable<Match> FindMatches(byte[] input, int position)
        {
            if (!_array.IsBuilt)
                _array.Build(input, 0, MinMatchSize);

            if (input.Length - position < MinMatchSize)
                yield break;

            var maxSize = Math.Min(MaxMatchSize, input.Length - position);
            var longestMatchSize = MinMatchSize - 1;
            var displacement = -1;
            var offsets = _array.GetOffsets(position, MinMatchSize, MinDisplacement, MaxDisplacement, DataType);
            foreach (var offset in offsets.OrderByDescending(x => x))
            {
                var matchMaxSize = maxSize;
                if(!UseLookAhead)
                    matchMaxSize= Math.Min(matchMaxSize, position - offset);

                var matchLength = MinMatchSize;
                while (matchLength < matchMaxSize && input[offset + matchLength] == input[position + matchLength])
                    matchLength++;

                if (matchLength > longestMatchSize)
                {
                    longestMatchSize = matchLength;
                    displacement = position - offset;
                    if (longestMatchSize == matchMaxSize)
                        break;
                }
            }

            if (displacement > -1)
                yield return new Match(position, displacement, longestMatchSize);
        }

        public void Dispose()
        {
            _array.Dispose();
        }
    }
}
