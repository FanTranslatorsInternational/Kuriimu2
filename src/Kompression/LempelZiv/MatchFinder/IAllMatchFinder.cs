using System;
using System.Collections.Generic;

namespace Kompression.LempelZiv.MatchFinder
{
    public interface IAllMatchFinder : IMatchFinder, IDisposable
    {
        IEnumerable<LzMatch> FindAllMatches(byte[] input, int position, int limit = -1);
    }
}
