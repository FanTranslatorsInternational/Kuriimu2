using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.LempelZiv.MatchFinder
{
    public interface IMatchFinder
    {
        int MinMatchSize { get; }
        int MaxMatchSize { get; }
    }
}
