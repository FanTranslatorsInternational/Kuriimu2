using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var inputArray = input.ToArray();
            var stopwatch = new Stopwatch();

            for (var i = 0; i < input.Length; i++)
            {
                // Get longest match at position i
                stopwatch.Restart();
                var match = _finder.FindLongestMatch(inputArray, i);
                stopwatch.Stop();
                Trace.WriteLine(stopwatch.Elapsed);

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
