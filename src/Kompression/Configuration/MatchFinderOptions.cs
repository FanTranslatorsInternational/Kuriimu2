using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.PatternMatch;

namespace Kompression.Configuration
{
    class MatchFinderOptions : IMatchFinderOptions
    {
        public IList<Func<IMatchFinder>> _matchFinderFactories;
        public bool FindBackwards { get; private set; }

        internal MatchFinderOptions() { }

        public IMatchFinderOptions FindInBackwardOrder()
        {
            FindBackwards = true;
            return this;
        }

        public IMatchFinderOptions FindMatchesWith(Func<IMatchFinder> matchFinderFactory)
        {
            if (_matchFinderFactories == null)
                _matchFinderFactories = new List<Func<IMatchFinder>>();

            _matchFinderFactories.Add(matchFinderFactory);
            return this;
        }
    }
}
