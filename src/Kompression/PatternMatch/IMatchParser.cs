using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.PatternMatch
{
    public interface IMatchParser : IDisposable
    {
        bool IsBackwards { get; }

        int PreBufferSize { get; }

        int SkipAfterMatch { get; }

        Match[] ParseMatches(Stream input);

        // TODO: Remove this method
        Match[] ParseMatches(byte[] input, int startPosition);
    }
}
