using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

namespace Kompression.Implementations
{
    public class LZ60:BaseLz
    {
        protected override IPatternMatchEncoder CreateEncoder()
        {
            return new Lz60Encoder();
        }

        protected override IMatchParser CreateParser(int inputLength)
        {
            // Yes, we do use the Lz40 price calculator
            return new NewOptimalParser(new Lz40PriceCalculator(),0,
                new HybridSuffixTreeMatchFinder(0x3, 0x1010F,1, 0xFFF));
        }

        protected override IPatternMatchDecoder CreateDecoder()
        {
            return new Lz60Decoder();
        }

        public override string[] Names => new[] { "Lz60" };
    }
}
