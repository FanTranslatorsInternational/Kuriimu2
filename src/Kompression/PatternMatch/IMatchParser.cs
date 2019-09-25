using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.PatternMatch
{
    public interface IMatchParser : IDisposable
    {
        int SkipAfterMatch { get; }

        Match[] ParseMatches(byte[] input, int startPosition);
    }
}
