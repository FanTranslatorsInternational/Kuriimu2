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
        private IMatchFinderOptions _matchFinderOptions;
        private IMatchParserOptions _matchParserOptions;

        public Func<IPriceCalculator> PriceCalculatorFactory;

        internal MatchOptions() { }

        public IMatchOptions CalculatePricesWith(Func<IPriceCalculator> priceCalculatorFactory)
        {
            PriceCalculatorFactory = priceCalculatorFactory;
            return this;
        }

        public IMatchOptions WithMatchFinderOptions(Action<IMatchFinderOptions> configure)
        {
            if (_matchFinderOptions == null)
                _matchFinderOptions = new MatchFinderOptions();
            configure(_matchFinderOptions);
            return this;
        }

        public IMatchOptions WithMatchParserOptions(Action<IMatchParserOptions> configure)
        {
            if (_matchParserOptions == null)
                _matchParserOptions = new MatchParserOptions();
            configure(_matchParserOptions);
            return this;
        }
    }
}
