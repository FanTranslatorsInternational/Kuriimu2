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
    public class Wp16 : BaseLz
    {
        protected override int PreBufferLength => 0x42;

        protected override IPatternMatchEncoder CreateEncoder()
        {
            return new Wp16Encoder();
        }

        protected override IMatchParser CreateParser(int inputLength)
        {
            return new NewOptimalParser(new Wp16PriceCalculator(), 0,
                new HistoryMatchFinder(4, 0x42, 2, 0xFFE, true, DataType.Short));
        }

        protected override IPatternMatchDecoder CreateDecoder()
        {
            return new Wp16Decoder();
        }

        public override string[] Names => new[] { "Wp16" };
    }
}
