using System;
using System.Collections.Generic;
using System.IO;

namespace Kompression.RunLengthEncoding.Encoders
{
    public interface IRleEncoder : IDisposable
    {
        void Encode(Stream input, Stream output, IList<IMatch> matches);
    }
}
