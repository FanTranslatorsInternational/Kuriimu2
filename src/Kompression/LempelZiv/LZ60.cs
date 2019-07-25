using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

namespace Kompression.LempelZiv
{
    public class LZ60:BaseLz
    {
        protected override ILzEncoder CreateEncoder()
        {
            return new Lz60Encoder();
        }

        protected override ILzParser CreateParser(int inputLength)
        {
            // TODO: Implement window based parser
            //return new NaiveParser(3, 0x10010F, 0xFFF);
            return new GreedyParser(new HybridSuffixTreeMatchFinder(0x3, 0x10010F, 0xFFF));
        }

        protected override ILzDecoder CreateDecoder()
        {
            return new Lz60Decoder();
        }
    }
}
