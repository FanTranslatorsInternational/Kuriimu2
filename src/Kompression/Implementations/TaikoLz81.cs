using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.MatchFinders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

namespace Kompression.Implementations
{
    public class TaikoLz81 : BaseLz
    {
        protected override IPatternMatchEncoder CreateEncoder()
        {
            return new TaikoLz81Encoder();
        }

        protected override IMatchParser CreateParser(int inputLength)
        {
            return new NewOptimalParser(new TaikoLz81PriceCalculator(), 0, 
                new HistoryMatchFinder(1, 0x102, 2, 0x8000));
        }

        protected override IPatternMatchDecoder CreateDecoder()
        {
            return new TaikoLz81Decoder();
        }

        public override string[] Names => new[] { "Namco Vita Mode 0x81" };

    }
}
