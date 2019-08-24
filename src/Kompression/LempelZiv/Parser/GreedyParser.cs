using System.Collections.Generic;
using Kompression.LempelZiv.MatchFinder;

namespace Kompression.LempelZiv.Parser
{
    /// <summary>
    /// Searches for longest pattern matches at position i and jumps over them.
    /// </summary>
    public class GreedyParser : ILzParser
    {
        private readonly ILongestMatchFinder _finder;

        public int SkipAfterMatch { get; }

        /// <summary>
        /// Creates a new instance of <see cref="GreedyParser"/>.
        /// </summary>
        /// <param name="finder">The <see cref="ILongestMatchFinder"/> to find a longest pattern match at a given position.</param>
        /// <param name="skipAfterMatch">The bytes to skip after a found pattern match.</param>
        public GreedyParser(ILongestMatchFinder finder, int skipAfterMatch = 0)
        {
            _finder = finder;
            SkipAfterMatch = skipAfterMatch;
        }

        /// <inheritdoc cref="Parse"/>
        public Match[] Parse(byte[] input, int startPosition)
        {
            var results = new List<Match>();

            var positionOffset = 0;
            while (positionOffset + startPosition < input.Length)
            {
                // Get longest match at position i
                var match = _finder.FindLongestMatch(input, startPosition + positionOffset);

                if (match != null)
                {
                    // Skip the whole pattern
                    results.Add(match);
                    positionOffset += (int)match.Length + SkipAfterMatch;
                    continue;
                }

                positionOffset++;
            }

            return results.ToArray();
        }

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool dispose)
        {
            if (dispose)
                _finder.Dispose();
        }

        #endregion
    }
}
