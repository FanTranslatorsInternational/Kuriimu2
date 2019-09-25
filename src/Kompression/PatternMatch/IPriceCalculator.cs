using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.PatternMatch
{
    public interface IPriceCalculator
    {
        int CalculateLiteralPrice(int value);
        int CalculateMatchPrice(Match match);
    }
}
