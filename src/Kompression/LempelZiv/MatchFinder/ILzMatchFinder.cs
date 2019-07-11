using System;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.MatchFinder
{
    public interface ILzMatchFinder : IDisposable
    {
        LzMatch[] FindMatches(Span<byte> input, int position);
    }
}
