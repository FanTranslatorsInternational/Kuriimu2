using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.PatternMatch;

namespace Kompression.Configuration
{
    public interface IMatchOptions
    {
        IMatchOptions CalculatePricesWith(Func<IPriceCalculator> priceCalculatorFactory);

        IMatchOptions WithMatchFinderOptions(Action<IMatchFinderOptions> configure);

        IMatchOptions WithMatchParserOptions(Action<IMatchParserOptions> configure);
    }
}
