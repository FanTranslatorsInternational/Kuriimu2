using System;
using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

namespace Kompression.Implementations
{
    /// <summary>
    /// Provides methods for handling Lz10 compression.
    /// </summary>
    //public class Lze : BaseLz
    //{
    //    protected override IPatternMatchEncoder CreateEncoder()
    //    {
    //        return new LzeEncoder();
    //    }

    //    protected override IMatchParser CreateParser(int inputLength)
    //    {
    //        return new NewOptimalParser(new LzePriceCalculator(), 0,
    //            new HybridSuffixTreeMatchFinder(0x3, 0x12, 5, 0x1004),
    //            new HybridSuffixTreeMatchFinder(0x2, 0x41, 1, 4));
    //    }

    //    protected override IPatternMatchDecoder CreateDecoder()
    //    {
    //        return new LzeDecoder();
    //    }

    //    public override string[] Names => new[] { "Lze" };
    //}
}
