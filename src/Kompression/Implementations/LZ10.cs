using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.MatchFinders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

namespace Kompression.Implementations
{
    /// <summary>
    /// Provides methods for handling Lz10 compression.
    /// </summary>
    //public class Lz10 : BaseLz
    //{
    //    protected override IPatternMatchEncoder CreateEncoder()
    //    {
    //        return new Lz10Encoder();
    //    }

    //    protected override IMatchParser CreateParser(int inputLength)
    //    {
    //        return new NewOptimalParser(new Lz10PriceCalculator(), 0,
    //            new HistoryMatchFinder(0x3, 0x12, 1, 0x1000));
    //    }

    //    protected override IPatternMatchDecoder CreateDecoder()
    //    {
    //        return new Lz10Decoder();
    //    }

    //    public override string[] Names => new[] { "Lz10" };
    //}
}
