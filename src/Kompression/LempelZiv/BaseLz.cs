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

        public Stream Decompress(Stream input)
        {
            return CreateDecoder().Decode(input);
        }

        public Stream Compress(Stream input)
        {
            var matchFinder = CreateMatchFinder();
            var encoder = CreateEncoder(CreateMatcher(matchFinder), matchFinder);
            return encoder.Encode(input);
        }
    }
}
