using System;

namespace Kompression.LempelZiv.MatchFinder
{
    public interface IAllMatchFinder : IMatchFinder, IDisposable
    {
        LzMatch[] FindAllMatches(Span<byte> input, int position);
    }
}
