using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.PatternMatch;

namespace Kompression.Configuration
{
    class MatchOptions : IMatchOptions
    {
        public IList<Func<IList<FindLimitations>, IMatchFinder>> MatchFinderFactories { get; private set; }

        public IList<Func<FindLimitations>> LimitFactories { get; private set; }

        public bool FindBackwards { get; private set; }

        public Func<IPriceCalculator> PriceCalculatorFactory { get; private set; }

        public Func<IList<IMatchFinder>, IPriceCalculator, int, IMatchParser> MatchParserFactory { get; private set; }

        public int PreBufferSize { get; private set; }

        public int SkipAfterMatch { get; private set; }

        internal MatchOptions() { }

        public IMatchOptions CalculatePricesWith(Func<IPriceCalculator> priceCalculatorFactory)
        {
            PriceCalculatorFactory = priceCalculatorFactory;
            return this;
        }

        public IMatchOptions FindInBackwardOrder()
        {
            FindBackwards = true;
            return this;
        }

        public IMatchOptions WithinLimitations(Func<FindLimitations> limitFactory)
        {
            if (LimitFactories == null)
                LimitFactories = new List<Func<FindLimitations>>();

            LimitFactories.Add(limitFactory);
            return this;
        }

        public IMatchOptions FindMatchesWith(Func<IList<FindLimitations>, IMatchFinder> matchFinderFactory)
        {
            if (MatchFinderFactories == null)
                MatchFinderFactories = new List<Func<IList<FindLimitations>, IMatchFinder>>();

            MatchFinderFactories.Add(matchFinderFactory);
            return this;
        }

        public IMatchOptions ParseMatchesWith(Func<IList<IMatchFinder>, IPriceCalculator, int, IMatchParser> matchParserFactory)
        {
            MatchParserFactory = matchParserFactory;
            return this;
        }

        public IMatchOptions WithPreBufferSize(int size)
        {
            PreBufferSize = size;
            return this;
        }

        public IMatchOptions SkipUnitsAfterMatch(int skip)
        {
            SkipAfterMatch = skip;
            return this;
        }
    }
}
