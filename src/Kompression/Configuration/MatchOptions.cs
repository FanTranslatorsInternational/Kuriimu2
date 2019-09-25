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
        public MatchFinderOptions MatchFinderOptions { get; private set; }

        public MatchParserOptions MatchParserOptions { get; private set; }

        public Func<IPriceCalculator> PriceCalculatorFactory { get; private set; }

        internal MatchOptions() { }

        public IMatchOptions CalculatePricesWith(Func<IPriceCalculator> priceCalculatorFactory)
        {
            PriceCalculatorFactory = priceCalculatorFactory;
            return this;
        }

        public IMatchOptions WithMatchFinderOptions(Action<IMatchFinderOptions> configure)
        {
            if (MatchFinderOptions == null)
                MatchFinderOptions = new MatchFinderOptions();
            configure(MatchFinderOptions);
            return this;
        }

        public IMatchOptions WithMatchParserOptions(Action<IMatchParserOptions> configure)
        {
            if (MatchParserOptions == null)
                MatchParserOptions = new MatchParserOptions();
            configure(MatchParserOptions);
            return this;
        }
    }
}
