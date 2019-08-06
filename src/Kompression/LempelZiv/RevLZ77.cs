using System;
using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.Parser;

namespace Kompression.LempelZiv
{
    public class RevLz77 : BaseLz
    {
        protected override ILzEncoder CreateEncoder()
        {
            throw new NotImplementedException();
        }

        protected override ILzParser CreateParser(int inputLength)
        {
            throw new NotImplementedException();
        }

        protected override ILzDecoder CreateDecoder()
        {
            return new RevLz77Decoder();
        }
    }
}
