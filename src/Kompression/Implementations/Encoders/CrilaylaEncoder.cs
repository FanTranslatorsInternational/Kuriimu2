using System;
using System.Collections.Generic;
using System.IO;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    public class CrilaylaEncoder : ILzEncoder
    {
        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
