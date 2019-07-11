using System;
using System.IO;

namespace Kompression.LempelZiv.Encoders
{
    public interface ILzEncoder : IDisposable
    {
        void Encode(Stream input, Stream output);
    }
}
