using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

/* Used in Super Robot Taizen Z and MTV archive */

namespace Kompression.Implementations
{
    public class LzssVlc : BaseLz
    {
        protected override IPatternMatchEncoder CreateEncoder()
        {
            return new LzssVlcEncoder();
        }

        protected override IMatchParser CreateParser(int inputLength)
        {
            return null;
            //return new NewOptimalParser(new LzssVlcPriceCalculator(), 0,
            //    new HybridSuffixTreeMatchFinder(4, inputLength, 1, inputLength));
        }

        protected override IPatternMatchDecoder CreateDecoder()
        {
            return new LzssVlcDecoder();
        }

        public override string[] Names => new[] { "LzssVle" };
    }
}
