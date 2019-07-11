using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.Matcher;
using Kompression.LempelZiv.MatchFinder;

namespace Kompression.LempelZiv
{
    public abstract class BaseLz : ICompression
    {
        protected abstract ILzMatchFinder CreateMatchFinder();
        protected abstract ILzMatcher CreateMatcher(ILzMatchFinder matchFinder);
        protected abstract ILzEncoder CreateEncoder(ILzMatcher matcher, ILzMatchFinder matchFinder);
        protected abstract ILzDecoder CreateDecoder();

        public void Decompress(Stream input, Stream output)
        {
            CreateDecoder().Decode(input, output);
        }

        public void Compress(Stream input, Stream output)
        {
            var matchFinder = CreateMatchFinder();
            var encoder = CreateEncoder(CreateMatcher(matchFinder), matchFinder);
            encoder.Encode(input, output);
        }
    }
}
