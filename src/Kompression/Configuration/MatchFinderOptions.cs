using System;
using System.Collections.Generic;
using Kompression.PatternMatch;

namespace Kompression.Configuration
{
    class MatchFinderOptions : IMatchFinderOptions
    {
        public IList<Func<IList<FindLimitations>, IMatchFinder>> MatchFinderFactories;
        public IList<Func<FindLimitations>> LimitFactories;
        public bool FindBackwards { get; private set; }

        internal MatchFinderOptions() { }

        public IMatchFinderOptions FindInBackwardOrder()
        {
            FindBackwards = true;
            return this;
        }

        public IMatchFinderOptions WithFindLimitations(Func<FindLimitations> limitFactory)
        {
            if (LimitFactories == null)
                LimitFactories = new List<Func<FindLimitations>>();

            LimitFactories.Add(limitFactory);
            return this;
        }

        public IMatchFinderOptions FindMatchesWith(Func<IList<FindLimitations>, IMatchFinder> matchFinderFactory)
        {
            if (MatchFinderFactories == null)
                MatchFinderFactories = new List<Func<IList<FindLimitations>, IMatchFinder>>();

            MatchFinderFactories.Add(matchFinderFactory);
            return this;
        }
    }
}
