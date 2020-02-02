using System;

namespace Kontract.Kompression
{
    public interface IMatchState : IDisposable
    {
        int CountLiterals(int position);

        int CountMatches(int position);

        bool HasMatches(int position);
    }
}
