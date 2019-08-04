using System;

namespace Kompression.LempelZiv.MatchFinder
{
    public interface ILongestMatchFinder : IMatchFinder, IDisposable
    {
        LzMatch FindLongestMatch(byte[] input, int position);
    }
}
