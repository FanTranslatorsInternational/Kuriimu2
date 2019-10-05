using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Configuration;
using Kompression.Extensions;
using Kompression.Interfaces;
using Kompression.IO;
using Kompression.Models;

namespace Kompression.PatternMatch.MatchParser
{
    /// <summary>
    /// Parse longest matches and skipping them.
    /// </summary>
    public class GreedyMatchParser : IMatchParser
    {
        private IMatchFinder[] _finders;

        /// <inheritdoc cref="FindOptions"/>
        public FindOptions FindOptions { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="GreedyMatchParser"/>.
        /// </summary>
        /// <param name="findOptions">The findOptions to parse pattern matches.</param>
        /// <param name="finders">The <see cref="IMatchFinder"/>s to find the longest pattern matches.</param>
        public GreedyMatchParser(FindOptions findOptions, params IMatchFinder[] finders)
        {
            if (finders.Any(x => x.FindOptions.UnitSize != finders[0].FindOptions.UnitSize))
                throw new InvalidOperationException("All match finder have to have the same unit size.");

            _finders = finders;
            FindOptions = findOptions;
        }

        /// <inheritdoc cref="ParseMatches"/>
        public IEnumerable<Match> ParseMatches(Stream input)
        {
            var toParse = input;

            if (FindOptions.PreBufferSize > 0)
                toParse = FindOptions.SearchBackwards ?
                    new ConcatStream(input, new MemoryStream(new byte[FindOptions.PreBufferSize])) :
                    new ConcatStream(new MemoryStream(new byte[FindOptions.PreBufferSize]), input);

            if (FindOptions.SearchBackwards)
                toParse = new ReverseStream(toParse, toParse.Length);

            return InternalParseMatches(toParse.ToArray(), FindOptions.PreBufferSize);
        }

        private IEnumerable<Match> InternalParseMatches(byte[] input, int startPosition)
        {
            var unitSize = (int)_finders[0].FindOptions.UnitSize;

            var positionOffset = 0;
            while (startPosition + positionOffset < input.Length)
            {
                var match = GetLongestMatch(input, startPosition + positionOffset);
                if (match.Length > 0)
                {
                    yield return match;

                    // Skip the whole pattern
                    positionOffset += (int)match.Length + FindOptions.SkipUnitsAfterMatch * unitSize;
                }
                else
                {
                    positionOffset += unitSize;
                }
            }
        }

        private Match GetLongestMatch(byte[] input, int position)
        {
            var foundMatch = new Match();
            foreach (var finder in _finders)
            {
                var match = finder.FindMatchesAtPosition(input, position).OrderByDescending(x => x.Length).FirstOrDefault();
                if (match.Length > foundMatch.Length)
                    foundMatch = match;
            }

            return foundMatch;
        }

        #region Dispose

        public void Dispose()
        {
            foreach (var finder in _finders)
                finder?.Dispose();

            _finders = null;
            FindOptions = null;
        }

        #endregion
    }
}
