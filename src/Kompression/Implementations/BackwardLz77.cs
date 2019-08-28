using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.MatchFinders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

namespace Kompression.Implementations
{
    public class BackwardLz77 : BaseLz
    {
        protected override bool IsBackwards => true;

        protected override IPatternMatchEncoder CreateEncoder()
        {
            return new BackwardLz77Encoder(ByteOrder.LittleEndian);
        }

        protected override IMatchParser CreateParser(int inputLength)
        {
            return new NewOptimalParser(new BackwardLz77PriceCalculator(), 0,
                new BackwardLz77MatchFinder(3, 0x12, 3, 0x1002));
        }

        protected override IPatternMatchDecoder CreateDecoder()
        {
            return new BackwardLz77Decoder(ByteOrder.LittleEndian);
        }

        public override string[] Names => new[] { "BackwardLz77", "LzOvl", "BLZ" };
    }
}
