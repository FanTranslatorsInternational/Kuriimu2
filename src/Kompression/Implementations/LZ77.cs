using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.MatchFinders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

/* Is more LZSS, described by wikipedia, through the flag denoting if following data is compressed or raw.
   Though the format is denoted as LZ77 with the magic num? (Issue 517)*/

namespace Kompression.Implementations
{
    //public class LZ77 : BaseLz
    //{
    //    protected override IPatternMatchEncoder CreateEncoder()
    //    {
    //        return new Lz77Encoder();
    //    }

    //    protected override IMatchParser CreateParser(int inputLength)
    //    {
    //        return new NewOptimalParser(new Lz77PriceCalculator(), 1,
    //            new HybridSuffixTreeMatchFinder(0x1, 0xFF, 1, 0xFF));
    //    }

    //    protected override IPatternMatchDecoder CreateDecoder()
    //    {
    //        return new Lz77Decoder();
    //    }

    //    public override string[] Names => new[] { "Lz77" };
    //}
}
