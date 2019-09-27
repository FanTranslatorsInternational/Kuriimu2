using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Implementations.Decoders;
using Kompression.Implementations.MatchFinders;
using Kompression.PatternMatch;
using Kompression.PatternMatch.LempelZiv;

namespace Kompression.Implementations
{
    public class TalesOf03 : BaseLz
    {
        public TalesOf03()
        {
        }

        protected override IPatternMatchEncoder CreateEncoder()
        {
            throw new NotImplementedException();
        }

        protected override IMatchParser CreateParser(int inputLength)
        {
            return null;
            //return new NewOptimalParser(null, 0, new HistoryMatchFinder(0, 0, 0, 0));
        }

        protected override IPatternMatchDecoder CreateDecoder()
        {
            return new TalesOf03Decoder();
        }

        public override string[] Names => new[] { "Tales Of Mode 0x03" };
    }
}
