using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Extensions;
using Kompression.IO;

namespace Kompression.PatternMatch.LempelZiv
{
    /// <summary>
    /// Searches for longest pattern matches at position n and n+1 in one loop execution.
    /// </summary>
    public class PlusOneGreedyParser : IMatchParser
    {
        private IMatchFinder _finder;

        public bool IsBackwards { get; }

        public int PreBufferSize { get; }

        public int SkipAfterMatch { get; }

        /// <summary>
        /// Creates a new instance of <see cref="PlusOneGreedyParser"/>.
        /// </summary>
        /// <param name="finder">The <see cref="IMatchFinder"/> to find a longest pattern match at a given position.</param>
        /// <param name="skipAfterMatch"></param>
        public PlusOneGreedyParser(IMatchFinder finder, bool isBackwards, int preBufferSize, int skipAfterMatch)
        {
            _finder = finder;

            SkipAfterMatch = skipAfterMatch;
            PreBufferSize = preBufferSize;
            IsBackwards = isBackwards;
        }

        public Match[] ParseMatches(Stream input)
        {
            var toParse = input;

            if (PreBufferSize > 0)
                toParse = IsBackwards ?
                    new ConcatStream(input, new MemoryStream(new byte[PreBufferSize])) :
                    new ConcatStream(new MemoryStream(new byte[PreBufferSize]), input);

            if (IsBackwards)
                toParse = new ReverseStream(toParse, toParse.Length);

            return ParseMatches(toParse.ToArray(), PreBufferSize);
        }

        /// <inheritdoc cref="ParseMatches"/>
        public Match[] ParseMatches(byte[] input, int startPosition)
        {
            var results = new List<Match>();

            var positionOffset = 0;
            while (startPosition + positionOffset < input.Length)
            {
                // Find match at positionOffset
                var match = _finder.FindMatches(input, startPosition + positionOffset).FirstOrDefault();

                // If legit match was found
                if (match != null)
                {
                    // Find match at positionOffset
                    var matchPlusOne = _finder.FindMatches(input, startPosition + positionOffset + (int)_finder.DataType).FirstOrDefault();

                    // If legit 2nd match was found
                    if (matchPlusOne != null)
                    {
                        // If 2nd match is longer than 1st
                        if (match.Length + (int)_finder.DataType < matchPlusOne.Length)
                        {
                            results.Add(matchPlusOne);
                            positionOffset += (int)matchPlusOne.Length + SkipAfterMatch * (int)_finder.DataType;
                            continue;
                        }
                    }

                    // If 2nd match was not legit or not better
                    results.Add(match);
                    positionOffset += (int)match.Length + SkipAfterMatch * (int)_finder.DataType;
                    continue;
                }

                // If no legit matches were found
                positionOffset += (int)_finder.DataType;
            }

            return results.ToArray();
        }

        public void Dispose()
        {
            _finder.Dispose();
            _finder = null;
        }
    }
}
