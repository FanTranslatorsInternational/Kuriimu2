using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

/* Used in Super Robot Taizen Z and MTV archive */

namespace Kompression.LempelZiv
{
    public class LzssVlc : BaseLz
    {
        protected override bool IsBackwards => false;

        protected override ILzEncoder CreateEncoder()
        {
            return new LzssVlcEncoder();
        }

        protected override ILzParser CreateParser(int inputLength)
        {
            //return new OptimalParser(new HybridSuffixTreeMatchFinder(4, inputLength), new LzssVlcPriceCalculator());
            return new GreedyParser(new HybridSuffixTreeMatchFinder(4, inputLength, inputLength,1));
        }

        protected override ILzDecoder CreateDecoder()
        {
            return new LzssVlcDecoder();
        }
    }
}
