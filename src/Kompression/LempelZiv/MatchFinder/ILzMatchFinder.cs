using System;

namespace Kompression.LempelZiv.MatchFinder
{
    public interface ILzMatchFinder : IDisposable
    {
        int MinMatchSize { get; }
        int MaxMatchSize { get; }

        LzMatch FindLongestMatch(byte[] input, int position);
        LzMatch[] FindAllMatches(Span<byte> input, int position);
    }
}
