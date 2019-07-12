using System;
using System.IO;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.Parser
{
    public interface ILzParser : IDisposable
    {
        LzMatch[] Parse(Span<byte> input);
    }
}
