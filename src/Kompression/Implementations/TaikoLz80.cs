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
    public class TaikoLz80 : BaseLz
    {
        protected override IPatternMatchEncoder CreateEncoder()
        {
            return new TaikoLz80Encoder();
        }

        protected override IMatchParser CreateParser(int inputLength)
        {
            return null;
            //return new NewOptimalParser(new TaikoLz80PriceCalculator(), 0,
            //    new HistoryMatchFinder(2, 5, 1, 0x10),
            //    new HistoryMatchFinder(3, 0x12, 1, 0x400),
            //    new HistoryMatchFinder(4, 0x83, 1, 0x8000));
        }

        protected override IPatternMatchDecoder CreateDecoder()
        {
            return new TaikoLz80Decoder();
        }

        public override string[] Names => new[] { "Namco Vita Mode 0x80" };

    }
}
