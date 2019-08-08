using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

namespace Kompression.LempelZiv
{
    public class BackwardLz77 : BaseLz
    {
        protected override bool IsBackwards => true;

        protected override ILzEncoder CreateEncoder()
        {
            return new BackwardLz77Encoder(ByteOrder.LittleEndian);
        }

        protected override ILzParser CreateParser(int inputLength)
        {
            return new PlusOneGreedyParser(new NeedleHaystackMatchFinder(3, 0x12, 0x1002,3));
        }

        protected override ILzDecoder CreateDecoder()
        {
            return new BackwardLz77Decoder(ByteOrder.LittleEndian);
        }
    }
}
