using System;
using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

namespace Kompression.LempelZiv
{
    public class Yay0LE : BaseLz
    {
        protected override ILzEncoder CreateEncoder()
        {
            return new Yay0Encoder(ByteOrder.LittleEndian);
        }

        protected override ILzParser CreateParser(int inputLength)
        {
            return new PlusOneGreedyParser(new NeedleHaystackMatchFinder(3, 0x111, 0x1000,1));
        }

        protected override ILzDecoder CreateDecoder()
        {
            throw new NotImplementedException();
        }
    }
}
