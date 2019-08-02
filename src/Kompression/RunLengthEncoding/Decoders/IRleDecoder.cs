using System;
using System.IO;

namespace Kompression.RunLengthEncoding.Decoders
{
    public interface IRleDecoder : IDisposable
    {
        void Decode(Stream input, Stream output);
    }
}
