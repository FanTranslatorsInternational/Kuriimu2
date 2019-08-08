using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

/* The same as LZ40 just with another magic num */

namespace Kompression.LempelZiv
{
    public class LZ40:BaseLz
    {
        protected override ILzEncoder CreateEncoder()
        {
            return new Lz40Encoder();
        }

        protected override ILzParser CreateParser(int inputLength)
        {
            // TODO: Implement window based parser
            //return new NaiveParser(3, 0x10010F, 0xFFF);
            return new GreedyParser(new HybridSuffixTreeMatchFinder(0x3, 0x10010F, 0xFFF,1));
        }

        protected override ILzDecoder CreateDecoder()
        {
            return new Lz40Decoder();
        }
    }
}
