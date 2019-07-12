using System;
using System.Collections.Generic;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.Parser
{
    public class GreedyParser : ILzParser
    {
        private readonly ILzMatchFinder _finder;

        public GreedyParser(ILzMatchFinder finder)
        {
            _finder = finder;
        }

        public LzMatch[] Parse(Span<byte> input)
        {
            var results = new List<LzMatch>();

            for (var i = 0; i < input.Length; i++)
            {
                // Get longest match at position i
                var match = _finder.FindLongestMatch(input, i);
                if (match == null)
                    continue;

                // Skip the whole pattern
                results.Add(match);
                i += match.Length - 1;
            }

            return results.ToArray();
        }

        #region

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
