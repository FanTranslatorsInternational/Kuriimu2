using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.PatternMatch;

namespace Kompression.Configuration
{
    public interface IMatchParserOptions
    {
        IMatchParserOptions ParseMatchesWith(
            Func<IList<IMatchFinder>, IPriceCalculator, int, IMatchParser> matchParserFactory);

        IMatchParserOptions WithPreBufferSize(int size);

        IMatchParserOptions SkipUnitsAfterMatch(int skip);
    }
}
