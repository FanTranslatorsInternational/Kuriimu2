using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

namespace Kompression.Implementations
{
    public class Mio0BE : BaseLz
    {
        protected override IPatternMatchEncoder CreateEncoder()
        {
            return new Mio0Encoder(ByteOrder.BigEndian);
        }

        protected override IMatchParser CreateParser(int inputLength)
        {
            return new NewOptimalParser(new Mio0PriceCalculator(), 0,
                new HybridSuffixTreeMatchFinder(3, 0x12, 1, 0x1000));
        }

        protected override IPatternMatchDecoder CreateDecoder()
        {
            return new Mio0Decoder(ByteOrder.BigEndian);
        }

        public override string[] Names => new[] { "Mio0 BigEndian" };
    }
}
