using System;
using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

namespace Kompression.Implementations
{
    public class Yaz0BE : BaseLz
    {
        protected override IPatternMatchEncoder CreateEncoder()
        {
            return new Yaz0Encoder(ByteOrder.BigEndian);
        }

        protected override IMatchParser CreateParser(int inputLength)
        {
            // Yes, we do use the Yay0 price calculator
            return new NewOptimalParser(new Yay0PriceCalculator(),0,
                new HybridSuffixTreeMatchFinder(3, 0x111,1, 0x1000));
        }

        protected override IPatternMatchDecoder CreateDecoder()
        {
            return new Yaz0Decoder(ByteOrder.BigEndian);
        }

        public override string[] Names => new[] { "Yaz0 BigEndian" };
    }
}
