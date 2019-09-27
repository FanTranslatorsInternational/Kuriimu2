using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

/* The same as Lz10 just with another compression header */

namespace Kompression.Implementations
{
    public class LZSS : BaseLz
    {
        protected override IPatternMatchEncoder CreateEncoder()
        {
            return new LzssEncoder();
        }

        protected override IMatchParser CreateParser(int inputLength)
        {
            // Yes, we do use the Lz10 price calculator
            return null;
            //return new NewOptimalParser(new Lz10PriceCalculator(), 0, 
            //    new HybridSuffixTreeMatchFinder(0x3, 0x12, 1, 0x1000));
        }

        protected override IPatternMatchDecoder CreateDecoder()
        {
            return new LzssDecoder();
        }

        public override string[] Names => new[] { "Lzss" };
    }
}
