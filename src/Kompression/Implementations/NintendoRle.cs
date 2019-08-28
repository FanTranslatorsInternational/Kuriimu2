using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.PatternMatch;
using Kompression.PatternMatch.RunLength;

namespace Kompression.Implementations
{
    public class NintendoRle : BaseRle
    {
        protected override IPatternMatchEncoder CreateEncoder()
        {
            return new NintendoRleEncoder();
        }

        protected override IMatchFinder CreateMatchFinder()
        {
            return new RleMatchFinder(3, 0x82);
        }

        protected override IPatternMatchDecoder CreateDecoder()
        {
            return new NintendoRleDecoder();
        }

        public override string[] Names => new[] { "Nintendo Rle" };
    }
}
