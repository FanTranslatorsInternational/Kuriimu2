using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

namespace Kompression.LempelZiv
{
    /// <summary>
    /// Provides methods for handling Lz10 compression.
    /// </summary>
    public class Lz10 : BaseLz
    {
        protected override ILzEncoder CreateEncoder()
        {
            return new Lz10Encoder();
        }

        protected override ILzParser CreateParser(int inputLength)
        {
            // TODO: Implement window based parser
            //return new NaiveParser(3, 0x12, 0x1000);
            return new GreedyParser(new HybridSuffixTreeMatchFinder(0x3, 0x1000, 0x12));
        }

        protected override ILzDecoder CreateDecoder()
        {
            return new Lz10Decoder();
        }
    }
}
