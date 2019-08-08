using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.LempelZiv.MatchFinder;

namespace Kompression.LempelZiv.Parser
{
    public class PlusOneGreedyParser : ILzParser
    {
        private readonly ILongestMatchFinder _finder;

        public PlusOneGreedyParser(ILongestMatchFinder finder)
        {
            _finder = finder;
        }

        public LzMatch[] Parse(Span<byte> input, int startPosition)
        {
            var results = new List<LzMatch>();

            for (var i = startPosition; i < input.Length; i++)
            {
                // Find longest match at position i
                var match = _finder.FindLongestMatch(input.ToArray(), i);

                // If legit match was found
                if (match != null)
                {
                    // Find longest match at position i+1
                    var plusOneMatch = _finder.FindLongestMatch(input.ToArray(), i + 1);

                    // If legit match was found and 2nd match is longer than 1st
                    if (plusOneMatch != null && match.Length + 1 < plusOneMatch.Length)
                        // Use 2nd match, since it's longer
                        match = plusOneMatch;

                    results.Add(match);
                    i += match.Length - 1;
                }
            }

            return results.ToArray();
        }

        public void Dispose()
        {
            _finder.Dispose();
        }
    }
}
