using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.Configuration;
using Kompression.Interfaces;
using Kompression.Models;
using Kompression.PatternMatch.LempelZiv.Support;

namespace Kompression.PatternMatch.LempelZiv
{
    /// <summary>
    /// Finding matches using <see cref="SuffixArray"/>.
    /// </summary>
    public class SuffixArrayMatchFinder : IMatchFinder
    {
        private readonly SuffixArray _array;

        /// <inheritdoc cref="FindLimitations"/>
        public FindLimitations FindLimitations { get; private set; }

        /// <inheritdoc cref="FindOptions"/>
        public FindOptions FindOptions { get; private set; }

        public SuffixArrayMatchFinder(FindLimitations limits, FindOptions options)
        {
            _array = new SuffixArray();

            FindLimitations = limits;
            FindOptions = options;
        }

        // TODO: Check Suffix array get matches method
        public IEnumerable<Match> FindMatchesAtPosition(byte[] input, int position)
        {
            var maxSize = FindLimitations.MaxLength <= 0 ? input.Length : FindLimitations.MaxLength;
            var maxDisplacement = FindLimitations.MaxDisplacement <= 0 ? input.Length : FindLimitations.MaxDisplacement;

            if (!_array.IsBuilt)
                _array.Build(input, 0, FindLimitations.MinLength);

            if (input.Length - position < FindLimitations.MinLength)
                yield break;

            var cappedSize = Math.Min(maxSize, input.Length - position);
            var longestMatchSize = FindLimitations.MinLength - 1;
            var displacement = -1;
            var offsets = _array.GetOffsets(position, FindLimitations.MinLength, FindLimitations.MinDisplacement, maxDisplacement, (int)FindOptions.UnitSize);
            foreach (var offset in offsets.OrderByDescending(x => x))
            {
                var matchLength = FindLimitations.MinLength;
                while (matchLength < cappedSize && input[offset + matchLength] == input[position + matchLength])
                    matchLength++;

                if (matchLength > longestMatchSize)
                {
                    longestMatchSize = matchLength;
                    displacement = position - offset;
                    if (longestMatchSize == cappedSize)
                        break;
                }
            }

            if (displacement > -1)
                yield return new Match(position, displacement, longestMatchSize);
        }

        // TODO
        public IEnumerable<Match> GetAllMatches(byte[] input, int position)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _array.Dispose();
        }
    }
}
