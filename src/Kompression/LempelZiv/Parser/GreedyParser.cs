using System;
using System.Collections.Generic;
using Kompression.LempelZiv.MatchFinder;

namespace Kompression.LempelZiv.Parser
{
    public class GreedyParser : ILzParser
    {
        private readonly ILongestMatchFinder _finder;

        public int SkipAfterMatch { get; }

        public GreedyParser(ILongestMatchFinder finder, int skipAfterMatch = 0)
        {
            _finder = finder;
            SkipAfterMatch = skipAfterMatch;
        }

        public LzMatch[] Parse(byte[] input,int startPosition)
        {
            var results = new List<LzMatch>();

            for (var i = startPosition; i < input.Length; i++)
            {
                // Get longest match at position i
                var match = _finder.FindLongestMatch(input, i);

                if (match == null)
                    continue;

                // Skip the whole pattern
                results.Add(match);
                i += match.Length + SkipAfterMatch - 1;
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
