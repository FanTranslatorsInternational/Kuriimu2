using Kompression.LempelZiv.MatchFinder;
using Kompression.RunLengthEncoding.Decoders;
using Kompression.RunLengthEncoding.Encoders;
using Kompression.RunLengthEncoding.RleMatchFinders;

namespace Kompression.RunLengthEncoding
{
    public class NintendoRle : BaseRle
    {
        protected override IRleEncoder CreateEncoder()
        {
            return new NintendoRleEncoder();
        }

        protected override ILongestMatchFinder CreateMatchFinder()
        {
            return new RleMatchFinder(3, 0x82);
        }

        protected override IRleDecoder CreateDecoder()
        {
            return new NintendoRleDecoder();
        }
    }
}
