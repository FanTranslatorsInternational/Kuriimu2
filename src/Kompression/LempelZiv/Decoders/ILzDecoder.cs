using System;
using System.IO;

namespace Kompression.LempelZiv.Decoders
{
    public interface ILzDecoder : IDisposable
    {
        void Decode(Stream input, Stream output);
    }
}
