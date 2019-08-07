using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

namespace Kompression.LempelZiv
{
    public class LZ11 : BaseLz
    {
        protected override bool IsBackwards => false;

        protected override ILzEncoder CreateEncoder()
        {
            return new Lz11Encoder();
        }

        protected override ILzParser CreateParser(int inputLength)
        {
            // TODO: Implement window based parser
            //return new NaiveParser(3, 0x100110, 0x1000);
            return new GreedyParser(new HybridSuffixTreeMatchFinder(0x3, 0x100110, 0x1000,1));
        }

        protected override ILzDecoder CreateDecoder()
        {
            return new Lz11Decoder();
        }
    }
}
