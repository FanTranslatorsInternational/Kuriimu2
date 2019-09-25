using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.PatternMatch;

namespace Kompression.Configuration
{
    class MatchParserOptions : IMatchParserOptions
    {
        public Func<IMatchFinder, IPriceCalculator, IMatchParserOptions> MatchParserFactory { get; private set; }

        public int PreBufferSize { get; private set; }

        public int SkipAfterMatch { get; private set; }

        internal MatchParserOptions() { }

        public IMatchParserOptions ParseMatchesWith(Func<IMatchFinder, IPriceCalculator, IMatchParserOptions> matchParserFactory)
        {
            MatchParserFactory = matchParserFactory;
            return this;
        }

        public IMatchParserOptions WithPreBufferSize(int size)
        {
            PreBufferSize = size;
            return this;
        }

        public IMatchParserOptions SkipUnitsAfterMatch(int skip)
        {
            SkipAfterMatch = skip;
            return this;
        }
    }
}
