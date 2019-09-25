using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.PatternMatch;

namespace Kompression.Configuration
{
    public interface IMatchFinderOptions
    {
        IMatchFinderOptions FindInBackwardOrder();

        IMatchFinderOptions WithFindLimitations(Func<FindLimitations> limitFactory);

        IMatchFinderOptions FindMatchesWith(Func<IList<FindLimitations>, IMatchFinder> matchFinderFactory);
    }
}
