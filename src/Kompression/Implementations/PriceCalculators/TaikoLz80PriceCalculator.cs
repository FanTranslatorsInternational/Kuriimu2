using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.PatternMatch;

namespace Kompression.Implementations.PriceCalculators
{
    class TaikoLz80PriceCalculator : IPriceCalculator
    {
        public int CalculateLiteralPrice(int value)
        {
            // One raw value is stored as 8 bit, more than 1 value is batched together in 1 to 2 length bytes
            // To approximate some flag bits to each raw value, we take half of the raw flag (which is 2 zero bits) and add it to the price
            // In the best case, this one added bit to each raw value accumulates up enough to have an approximation of the used flag bits per raw block
            return 8 + 1;
        }

        public int CalculateMatchPrice(Match match)
        {
            /*var length = ((code >> 4) & 0x3) + 2;
            var displacement = (code & 0xF) + 1;*/
            if (match.Displacement <= 0x10 && match.Length <= 0x5)
                return 8;

            /*var length = ((code >> 2) & 0xF) + 3;
            var displacement = (((code & 0x3) << 8) | byte1) + 1;*/
            if (match.Displacement <= 0x400 && match.Length <= 0x12)
                return 16;

            return 24;
        }
    }
}
