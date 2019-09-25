using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.PatternMatch;

namespace Kompression.Implementations.PriceCalculators
{
    class Wp16PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value)
        {
            return 8 + 8 + 1;
        }

        public int CalculateMatchPrice(Match match)
        {
            return 1 + 8 + 8;
        }
    }
}
