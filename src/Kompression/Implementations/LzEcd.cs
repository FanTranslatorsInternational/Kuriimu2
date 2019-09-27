using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

namespace Kompression.Implementations
{
    //public class LzEcd : BaseLz
    //{
    //    protected override int PreBufferLength => 0x3BE;    // Taken from reverse engineered code

    //    protected override IPatternMatchEncoder CreateEncoder()
    //    {
    //        return new LzEcdEncoder(PreBufferLength);
    //    }

    //    protected override IMatchParser CreateParser(int inputLength)
    //    {
    //        return new NewOptimalParser(new LzEcdPriceCalculator(),0,
    //            new HybridSuffixTreeMatchFinder(3, 0x42, 1, 0x400));
    //    }

    //    protected override IPatternMatchDecoder CreateDecoder()
    //    {
    //        return new LzEcdDecoder(PreBufferLength);
    //    }

    //    public override string[] Names => new[] { "LzEcd" };
    //}
}
