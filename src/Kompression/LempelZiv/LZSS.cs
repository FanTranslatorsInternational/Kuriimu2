using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

/* The same as Lz10 just with another compression header */

namespace Kompression.LempelZiv
{
    public class LZSS:BaseLz
    {
        protected override bool IsBackwards => false;

        protected override ILzEncoder CreateEncoder()
        {
            return new LzssEncoder();
        }

        protected override ILzParser CreateParser(int inputLength)
        {
            // TODO: Implement window based parser
            //return new NaiveParser(3, 0x12, 0x1000);
            return new GreedyParser(new HybridSuffixTreeMatchFinder(0x3, 0x1000, 0x12,1));
        }

        protected override ILzDecoder CreateDecoder()
        {
            return new LzssDecoder();
        }
    }
}
