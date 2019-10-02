using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.Interfaces
{
    public interface IMatchState : IDisposable
    {
        int CountLiterals(int position);

        int CountMatches(int position);
    }
}
