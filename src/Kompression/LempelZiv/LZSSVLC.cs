using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.Matcher;
using Kompression.LempelZiv.MatchFinder;

/* Used in Super Robot Taizen Z and MTV archive */
// TODO: Find out that PS2 game from IcySon55

namespace Kompression.LempelZiv
{
    public class LzssVlc : BaseLz
    {
        protected override ILzMatchFinder CreateMatchFinder()
        {
            return new SuffixTreeMatcher(4, -1);
        }

        protected override ILzMatcher CreateMatcher(ILzMatchFinder matchFinder)
        {
            return new GreedyMatcher(matchFinder);
        }

        protected override ILzEncoder CreateEncoder(ILzMatcher matcher, ILzMatchFinder matchFinder)
        {
            return new LzssVlcEncoder(matcher);
        }

        protected override ILzDecoder CreateDecoder()
        {
            return new LzssVlcDecoder();
        }
    }
}
