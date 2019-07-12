using System;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.MatchFinder
{
    public interface ILzMatchFinder : IDisposable
    {
        int MinMatchSize { get; }
        int MaxMatchSize { get; }

        LzMatch FindLongestMatch(Span<byte> input, int position);
    }
}
