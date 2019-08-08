using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

/* Is more LZSS, described by wikipedia, through the flag denoting if following data is compressed or raw.
   Though the format is denoted as LZ77 with the magic num? (Issue 517)*/

namespace Kompression.LempelZiv
{
    public class LZ77 : BaseLz
    {
        protected override ILzEncoder CreateEncoder()
        {
            return new Lz77Encoder();
        }

        protected override ILzParser CreateParser(int inputLength)
        {
            // TODO: Implement window based parser
            //return new NaiveParser(1, 0xFF, 0xFF);
            return new GreedyParser(new HybridSuffixTreeMatchFinder(0x1, 0xFF, 0xFF,1), 1);
        }

        protected override ILzDecoder CreateDecoder()
        {
            return new Lz77Decoder();
        }
    }
}
