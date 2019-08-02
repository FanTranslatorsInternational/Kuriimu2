using System;
using System.Collections.Generic;

namespace Kompression.RunLengthEncoding.RleMatchFinders
{
    public interface IRleMatchFinder : IDisposable
    {
        IList<RleMatch> FindAllMatches(byte[] input);
    }
}
