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
    class OptimalParserMatcher : ILzMatcher
    {
        private readonly ILzMatchFinder _finder;

        public OptimalParserMatcher(ILzMatchFinder finder)
        {
            _finder = finder;
        }

        // TODO: Implement finding optimal matches
        public LzMatch[] FindMatches(Stream input)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool dispose)
        {
            if (dispose)
                _finder.Dispose();
        }
    }
}
