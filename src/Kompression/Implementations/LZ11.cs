using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.MatchFinders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

namespace Kompression.Implementations
{
    public class LZ11 : BaseLz
    {
        protected override IPatternMatchEncoder CreateEncoder()
        {
            return new Lz11Encoder();
        }

        protected override IMatchParser CreateParser(int inputLength)
        {
            return new NewOptimalParser(new Lz11PriceCalculator(), 0,
                new HybridSuffixTreeMatchFinder(3, 0x10110, 1, 0x1000));
        }

        protected override IPatternMatchDecoder CreateDecoder()
        {
            return new Lz11Decoder();
        }

        public override string[] Names => new[] { "Lz11" };
    }
}
