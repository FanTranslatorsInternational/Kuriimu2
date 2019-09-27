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

        IMatchOptions FindInBackwardOrder();

        IMatchOptions WithinLimitations(Func<FindLimitations> limitFactory);

        IMatchOptions FindMatchesWith(Func<IList<FindLimitations>, IMatchFinder> matchFinderFactory);

        IMatchOptions ParseMatchesWith(Func<IList<IMatchFinder>, IPriceCalculator, bool, int, int, IMatchParser> matchParserFactory);

        IMatchOptions WithPreBufferSize(int size);

        IMatchOptions SkipUnitsAfterMatch(int skip);
    }
}
