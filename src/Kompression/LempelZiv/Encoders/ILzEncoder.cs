using System;
using System.IO;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.Encoders
{
    public interface ILzEncoder : ILengthCalculator, IDisposable
    {
        void Encode(Stream input, Stream output,LzMatch[] matches);
    }
}
