using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.LempelZiv.Matcher.Models;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.Matcher
{
    public class GreedyMatcher : ILzMatcher
    {
        private readonly ILzMatchFinder _finder;

        public GreedyMatcher(ILzMatchFinder finder)
        {
            _finder = finder;
        }

        public LzMatch[] FindMatches(Stream input)
        {
            var results = new List<LzMatch>();
            var data = ToArray(input);

            for (var i = 0; i < data.Length; i++)
            {
                // Get matches at position i
                var matches = _finder.FindMatches(data, i);
                if (!matches.Any())
                    continue;

                // Get longest match; If >1 matches have the same size, take the one with the smallest displacement
                var maxLength = matches.Max(x => x.Length);
                var longestMatch = matches.Where(x => x.Length == maxLength).OrderBy(x => x.Displacement).First();

                results.Add(longestMatch);
            }

            return results.ToArray();
        }

        private byte[] ToArray(Stream input)
        {
            var bkPos = input.Position;
            var inputArray = new byte[input.Length];
            input.Read(inputArray, 0, inputArray.Length);
            input.Position = bkPos;

            return inputArray;
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
